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
} from '@app/ui/sidebar';

export interface NavItem {
  readonly link: readonly string[];
  readonly label: string;
  readonly icon: string;
  readonly hint: string;
}

/** Presentational. The workspace section of the sidebar. */
@Component({
  selector: 'app-workspace-nav',
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
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './workspace-nav.html',
})
export class WorkspaceNav {
  public readonly items = input.required<readonly NavItem[]>();
  public readonly label = input('Workspace');

  public readonly navigate = output<void>();
}
