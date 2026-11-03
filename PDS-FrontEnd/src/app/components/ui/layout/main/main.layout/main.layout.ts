import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AdminLayoutComponent, LayoutService, SidebarMenuItem, UserProfile } from '@public-intelligence/ui-core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../../../../services/auth/auth.service';
import { isFeatureEnabled } from '../../../../../utils/feature-flags';
import { MenuItemWithPermissions, sideData } from './config';

@Component({
  selector: 'app-main.layout',
  imports: [AdminLayoutComponent],
  templateUrl: './main.layout.html',
})
export class MainLayout implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private layoutService = inject(LayoutService);
  private router = inject(Router);

  private actionSub?: Subscription;

  protected menuItems = signal<SidebarMenuItem[]>([]);
  protected branding = {
    appName: 'PDS',
    moduleName: 'Modulo',
    logoUrl: 'assets/images/cupula-gob-azul.webp',
  };
  protected userProfile = signal<UserProfile | null>(null);

  ngOnInit() {
    this.menuItems.set(this.getFilteredMenuItems());
    this.setUserProfile();

    this.layoutService.profileMenuItems.set([
      { label: 'Mi acceso', action: 'my-access' },
      { label: 'Cerrar sesión', action: 'logout', destructive: true },
    ]);

    this.actionSub = this.layoutService.profileMenuAction$.subscribe((action) => {
      if (action === 'logout') {
        this.authService.onLogout();
      } else if (action === 'my-access') {
        this.router.navigate(['/mi-acceso']);
      }
    });
  }


  ngOnDestroy() {
    this.actionSub?.unsubscribe();
  }

  private hasAnyPermission(itemPermissions: string[], userPermissions: string[]): boolean {
    return itemPermissions.some((p) => userPermissions.includes(p));
  }

  private filterItems(
    items: MenuItemWithPermissions[],
    userPermissions: string[],
  ): SidebarMenuItem[] {
    return items
      .filter((item) => !item.feature || isFeatureEnabled(item.feature))
      .filter(
        (item) =>
          !item.permissions?.length || this.hasAnyPermission(item.permissions, userPermissions),
      )
      .map(({ children, ...rest }) => ({
        ...rest,
        ...(children?.length ? { children: this.filterItems(children, userPermissions) } : {}),
      }));
  }

  private getFilteredMenuItems(): SidebarMenuItem[] {
    const permissions = this.authService.decodeToken()?.permissions || [];
    return this.filterItems(sideData, permissions);
  }

  private setUserProfile() {
    const token = this.authService.decodeToken();
    this.userProfile.set({
      name: token?.person_full_name || '',
      // El rol es role_name; la institución solo como respaldo si el token no lo trae.
      // Mostrar la institución en el lugar del rol confunde sobre qué puede hacer uno.
      role: token?.role_name || token?.organization_name || '',
      avatarUrl: 'assets/images/user-avatar.svg',
    });
  }
}
