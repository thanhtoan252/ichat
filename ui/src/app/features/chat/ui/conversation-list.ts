import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import {
  HlmSidebarGroup,
  HlmSidebarGroupContent,
  HlmSidebarGroupLabel,
  HlmSidebarMenu,
  HlmSidebarMenuButton,
  HlmSidebarMenuItem,
  HlmSidebarMenuSkeleton,
} from '@app/ui/sidebar';
import type { ConversationGroup } from '../util/conversation-groups';

/**
 * Presentational. Renders the grouped conversation history.
 *
 * Each item carries its own `link`, supplied by the container, so the rows stay real
 * anchors — middle-click and "open in new tab" keep working — without this component
 * knowing a single route of the application.
 */
@Component({
  selector: 'app-conversation-list',
  imports: [
    RouterLink,
    RouterLinkActive,
    NgIcon,
    HlmSidebarGroup,
    HlmSidebarGroupContent,
    HlmSidebarGroupLabel,
    HlmSidebarMenu,
    HlmSidebarMenuButton,
    HlmSidebarMenuItem,
    HlmSidebarMenuSkeleton,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './conversation-list.html',
})
export class ConversationList {
  public readonly groups = input.required<readonly ConversationGroup[]>();
  public readonly loading = input(false);
  public readonly emptyMessage = input('No conversations yet. Ask something to start one.');

  public readonly conversationSelect = output<string>();

  protected readonly skeletonRows = [0, 1, 2, 3, 4];
}
