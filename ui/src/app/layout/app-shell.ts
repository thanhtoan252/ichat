import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import {
  HlmSidebar,
  HlmSidebarContent,
  HlmSidebarFooter,
  HlmSidebarHeader,
  HlmSidebarInset,
  HlmSidebarRail,
  HlmSidebarService,
  HlmSidebarWrapper,
} from '@app/ui/sidebar';
import { ThemeService } from '@app/core/theme/theme.service';
import { ChatStore, ConversationList, groupConversations } from '@app/features/chat';
import { ThemeToggle } from './components/theme-toggle';
import { WorkspaceNav, type NavItem } from './components/workspace-nav';

const NAV_ITEMS: readonly NavItem[] = [
  {
    link: ['/documents'],
    label: 'Knowledge base',
    icon: 'lucideFileStack',
    hint: 'Documents and ingestion status',
  },
  {
    link: ['/retrieval'],
    label: 'Retrieval lab',
    icon: 'lucideLayers',
    hint: 'Inspect the hybrid search pipeline',
  },
  {
    link: ['/settings'],
    label: 'Settings',
    icon: 'lucideSettings',
    hint: 'Providers, models and appearance',
  },
];

/**
 * Container. Owns the sidebar: conversation history, workspace nav and theme.
 *
 * The history lives here rather than inside the chat route so it survives navigating to
 * Documents or the Retrieval lab and back.
 */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    NgIcon,
    HlmButton,
    HlmSidebar,
    HlmSidebarContent,
    HlmSidebarFooter,
    HlmSidebarHeader,
    HlmSidebarInset,
    HlmSidebarRail,
    HlmSidebarWrapper,
    ConversationList,
    ThemeToggle,
    WorkspaceNav,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly router = inject(Router);
  private readonly sidebar = inject(HlmSidebarService);

  protected readonly store = inject(ChatStore);
  protected readonly theme = inject(ThemeService);
  protected readonly navItems = NAV_ITEMS;

  protected readonly conversationGroups = computed(() =>
    groupConversations(this.store.conversations()),
  );

  /** An unreachable API must not read as "you have no conversations". */
  protected readonly conversationsEmptyMessage = computed(
    () => this.store.conversationsError() ?? 'No conversations yet. Ask something to start one.',
  );

  protected async startNewChat(): Promise<void> {
    const conversation = await this.store.startConversation();

    if (conversation) {
      await this.router.navigate(['/chat', conversation.id]);
    }

    this.closeOnMobile();
  }

  protected closeOnMobile(): void {
    if (this.sidebar.isMobile()) {
      this.sidebar.setOpenMobile(false);
    }
  }
}
