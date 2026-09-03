import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from './api.config';
import type {
  ChatMessage,
  ChatStreamEvent,
  Conversation,
  CreateConversationRequest,
  DeltaPayload,
  DonePayload,
  ErrorPayload,
  PagedResponse,
  SendMessageRequest,
  SourcesPayload,
  StatusPayload,
} from './api.models';
import { readSse } from './sse';

@Injectable({ providedIn: 'root' })
export class ConversationsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private get root(): string {
    return `${this.baseUrl}${API_V1}/conversations`;
  }

  list(page = 1, pageSize = 20, userId?: string): Promise<PagedResponse<Conversation>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (userId) {
      params = params.set('userId', userId);
    }

    return firstValueFrom(this.http.get<PagedResponse<Conversation>>(this.root, { params }));
  }

  create(request: CreateConversationRequest = {}): Promise<Conversation> {
    return firstValueFrom(this.http.post<Conversation>(this.root, request));
  }

  messages(conversationId: string, page = 1, pageSize = 50): Promise<PagedResponse<ChatMessage>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return firstValueFrom(
      this.http.get<PagedResponse<ChatMessage>>(`${this.root}/${conversationId}/messages`, {
        params,
      }),
    );
  }

  /**
   * Streams one answer. Every frame the API defines is mapped onto the
   * `ChatStreamEvent` union; anything unrecognised is dropped rather than guessed at,
   * so a future event type cannot crash an older client.
   */
  async *streamAnswer(
    conversationId: string,
    request: SendMessageRequest,
    signal: AbortSignal,
  ): AsyncGenerator<ChatStreamEvent, void, undefined> {
    const frames = readSse(`${this.root}/${conversationId}/messages`, {
      method: 'POST',
      body: JSON.stringify(request),
      headers: { 'Content-Type': 'application/json' },
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
