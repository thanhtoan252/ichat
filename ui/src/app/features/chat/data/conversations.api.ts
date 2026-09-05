import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from '@app/core/api/api.config';
import { readSse } from '@app/core/api/sse';
import { AuthStore } from '@app/core/auth/auth.store';
import type { PagedResponse } from '@app/shared/util/api-envelope.model';
import type {
  ChatMessageDto,
  ConversationDto,
  CreateConversationRequestDto,
  SendMessageRequestDto,
} from './conversations.dto';
import type {
  ChatStreamEvent,
  DeltaPayload,
  DonePayload,
  ErrorPayload,
  SourcesPayload,
  StatusPayload,
} from './chat-stream.model';

@Injectable({ providedIn: 'root' })
export class ConversationsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly auth = inject(AuthStore);

  private get root(): string {
    return `${this.baseUrl}${API_V1}/conversations`;
  }

  /** No owner parameter: the API scopes the list to the caller's own token. */
  list(page = 1, pageSize = 20): Promise<PagedResponse<ConversationDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return firstValueFrom(this.http.get<PagedResponse<ConversationDto>>(this.root, { params }));
  }

  create(request: CreateConversationRequestDto = {}): Promise<ConversationDto> {
    return firstValueFrom(this.http.post<ConversationDto>(this.root, request));
  }

  messages(
    conversationId: string,
    page = 1,
    pageSize = 50,
  ): Promise<PagedResponse<ChatMessageDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return firstValueFrom(
      this.http.get<PagedResponse<ChatMessageDto>>(`${this.root}/${conversationId}/messages`, {
        params,
      }),
    );
  }

  /**
   * Streams one answer. Every frame the API defines is mapped onto the
   * `ChatStreamEvent` union; anything unrecognised is dropped rather than guessed at,
   * so a future event type cannot crash an older client.
   *
   * The bearer token is attached here by hand: `readSse` talks to `fetch` directly
   * because the endpoint is a POST, so no `HttpClient` interceptor ever sees it.
   */
  async *streamAnswer(
    conversationId: string,
    request: SendMessageRequestDto,
    signal: AbortSignal,
  ): AsyncGenerator<ChatStreamEvent, void, undefined> {
    const accessToken = await this.auth.getAccessToken();

    const frames = readSse(`${this.root}/${conversationId}/messages`, {
      method: 'POST',
      body: JSON.stringify(request),
      headers: {
        'Content-Type': 'application/json',
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      },
      signal,
    });

    for await (const frame of frames) {
      const event = toStreamEvent(frame.event, frame.data);

      if (event) {
        yield event;
      }
    }
  }
}

function toStreamEvent(event: string, raw: string): ChatStreamEvent | null {
  let data: unknown;

  try {
    data = JSON.parse(raw);
  } catch {
    return null;
  }

  switch (event) {
    case 'status':
      return { type: 'status', data: data as StatusPayload };
    case 'sources':
      return { type: 'sources', data: data as SourcesPayload };
    case 'delta':
      return { type: 'delta', data: data as DeltaPayload };
    case 'done':
      return { type: 'done', data: data as DonePayload };
    case 'error':
      return { type: 'error', data: data as ErrorPayload };
    default:
      return null;
  }
}
