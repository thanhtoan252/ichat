export { CHAT_ROUTES } from './feature/chat.routes';

// The app shell (layout/) is not a feature, but it is outside chat/ and still needs to
// show conversation history and start new conversations from the sidebar — so it goes
// through this barrel like any other external consumer would, rather than reaching into
// chat's internals directly.
export { ChatStore } from './data-access/chat.store';
export { groupConversations } from './util/conversation-groups';
export { ConversationList } from './ui/conversation-list';
