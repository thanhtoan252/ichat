import { describe, expect, it } from 'vitest';
import type { Conversation } from './chat.model';
import { groupConversations } from './conversation-groups';

const NOW = new Date('2026-08-29T12:00:00Z').getTime();
const HOUR = 60 * 60 * 1000;

function conversation(id: string, ageHours: number): Conversation {
  return {
    id,
    title: `Conversation ${id}`,
    userId: 'u-1',
    createdAt: new Date(NOW - ageHours * HOUR).toISOString(),
    updatedAt: new Date(NOW - ageHours * HOUR).toISOString(),
  };
}

describe('groupConversations', () => {
  it('returns nothing for an empty history', () => {
    expect(groupConversations([], NOW)).toEqual([]);
  });

  it('buckets by recency', () => {
    const groups = groupConversations(
      [
        conversation('a', 2),
        conversation('b', 30),
        conversation('c', 100),
        conversation('d', 500),
        conversation('e', 2000),
      ],
      NOW,
    );

    expect(groups.map((group) => group.label)).toEqual([
      'Today',
      'Yesterday',
      'Previous 7 days',
      'Previous 30 days',
      'Older',
    ]);
  });

  it('keeps several conversations in the same bucket together, in order', () => {
    const groups = groupConversations([conversation('a', 1), conversation('b', 3)], NOW);

    expect(groups).toHaveLength(1);
    expect(groups[0].items.map((item) => item.id)).toEqual(['a', 'b']);
  });

  it('gives each item a router link the sidebar can use as an anchor', () => {
    const [group] = groupConversations([conversation('abc', 1)], NOW);

    expect(group.items[0].link).toEqual(['/chat', 'abc']);
  });

  it('formats a time label for each item', () => {
    const [group] = groupConversations([conversation('a', 1)], NOW);

    expect(group.items[0].timeLabel).toMatch(/^\d{2}:\d{2}$/);
  });

  it('treats the bucket edges as exclusive upper bounds', () => {
    const justUnderADay = groupConversations([conversation('a', 23.9)], NOW);
    const justOverADay = groupConversations([conversation('b', 24.1)], NOW);

    expect(justUnderADay[0].label).toBe('Today');
    expect(justOverADay[0].label).toBe('Yesterday');
  });
});
