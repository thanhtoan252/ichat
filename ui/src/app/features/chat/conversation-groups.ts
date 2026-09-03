import type { Conversation } from '@app/core/api/api.models';

/** One row as the sidebar renders it — no domain object reaches the presentational layer. */
export interface ConversationListItem {
  readonly id: string;
  readonly title: string;
  readonly timeLabel: string;
  readonly link: readonly string[];
}

export interface ConversationGroup {
  readonly label: string;
  readonly items: readonly ConversationListItem[];
}

const HOUR = 60 * 60 * 1000;

/**
 * Buckets conversations by recency.
 *
 * Grouping by "Today / Yesterday / …" is what makes a long history scannable — at sidebar
 * size the timestamps all look alike. Kept as a pure function so the container stays thin
 * and this stays directly testable.
 */
export function groupConversations(
  conversations: readonly Conversation[],
  now: number = Date.now(),
): readonly ConversationGroup[] {
  const groups = new Map<string, ConversationListItem[]>();

  for (const conversation of conversations) {
    const updatedAt = new Date(conversation.updatedAt);
    const label = bucketLabel(now - updatedAt.getTime());
    const item: ConversationListItem = {
      id: conversation.id,
      title: conversation.title,
      timeLabel: formatTime(updatedAt),
      link: ['/chat', conversation.id],
    };

    const existing = groups.get(label);

    if (existing) {
      existing.push(item);
    } else {
      groups.set(label, [item]);
    }
  }

  return [...groups].map(([label, items]) => ({ label, items }));
}

function bucketLabel(ageMs: number): string {
  if (ageMs < 24 * HOUR) {
    return 'Today';
  }

  if (ageMs < 48 * HOUR) {
    return 'Yesterday';
  }

  if (ageMs < 7 * 24 * HOUR) {
    return 'Previous 7 days';
  }

  if (ageMs < 30 * 24 * HOUR) {
    return 'Previous 30 days';
  }

  return 'Older';
}

function formatTime(value: Date): string {
  return value.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
}
