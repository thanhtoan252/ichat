// The app shell (layout/) is not a feature, but it still needs to show conversation
// history and start new conversations from the sidebar — visible on every route, not
// just /chat. Routing itself goes straight to chat.routes.ts (see app.routes.ts), like
// every other feature; this barrel exists solely for layout's cross-boundary need,
// so it reaches chat's public surface the same way any external consumer would rather
// than importing chat's internals directly.
export { ChatStore } from './state/chat.store';
export { groupConversations } from './data/conversation-groups';
export { ConversationList } from './components/conversation-list';
