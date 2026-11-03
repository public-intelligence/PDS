import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { UserPositionService } from '../../services/user-position/user-position.service';
import { UserPositionDto, UserPositionPagedResultDto } from '../../models/user-position.model';

@Component({
  selector: 'app-user-access',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PageShellComponent, DatePipe],
  template: `
    <app-page-shell
      title="Gestión de Usuarios y Roles"
      description="Listado institucional de usuarios con acceso al sistema PDS, cargos asociados y roles asignados."
    >
      <div class="space-y-6">
        <!-- Top Metrics Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div class="stat-card">
            <div class="flex items-center justify-between text-ink-subtle">
              <span class="stat-label">Usuarios Registrados</span>
              <i class="bi bi-people text-lg"></i>
            </div>
            <div class="stat-value mt-1">{{ totalCount() }}</div>
          </div>
          <div class="stat-card">
            <div class="flex items-center justify-between text-ink-subtle">
              <span class="stat-label">Páginas Totales</span>
              <i class="bi bi-layers text-lg"></i>
            </div>
            <div class="stat-value mt-1">{{ totalPages() }}</div>
          </div>
          <div class="stat-card">
            <div class="flex items-center justify-between text-brand-500">
              <span class="stat-label">Página Actual</span>
              <i class="bi bi-file-earmark-text text-lg"></i>
            </div>
            <div class="stat-value text-brand mt-1">{{ pageNumber() }} / {{ totalPages() || 1 }}</div>
          </div>
          <div class="stat-card">
            <div class="flex items-center justify-between text-success-text">
              <span class="stat-label">Control de Acceso</span>
              <i class="bi bi-shield-check text-lg"></i>
            </div>
            <div class="mt-1">
              <span class="badge-success font-semibold text-xs">Administrador (planning:admin)</span>
            </div>
          </div>
        </div>

        <!-- Main Content Card -->
        <div class="card p-5 space-y-4">
          <!-- Search & Filter Controls Header -->
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-border pb-4">
            <div class="flex-1 min-w-[16rem] max-w-md">
              <div class="relative">
                <input
                  type="text"
                  class="field-control pl-9 pr-8 text-sm"
                  placeholder="Buscar por cargo o puesto..."
                  [(ngModel)]="searchPosition"
                  (keyup.enter)="buscar()"
                />
                <i class="bi bi-search absolute left-3 top-2.5 text-ink-subtle text-sm"></i>
                <button
                  *ngIf="searchPosition"
                  type="button"
                  class="absolute right-2.5 top-2.5 text-ink-subtle hover:text-ink text-sm"
                  (click)="limpiarBusqueda()"
                >
                  <i class="bi bi-x-circle-fill"></i>
                </button>
              </div>
            </div>

            <div class="flex items-center gap-3">
              <button
                type="button"
                class="btn-primary btn-sm flex items-center gap-1.5"
                [disabled]="loading()"
                (click)="buscar()"
              >
                <i class="bi bi-search"></i> Buscar
              </button>
              <button
                type="button"
                class="btn-secondary btn-sm flex items-center gap-1.5"
                [disabled]="loading()"
                (click)="cargarUsuarios()"
              >
                <i class="bi bi-arrow-clockwise"></i> Recargar
              </button>
            </div>
          </div>

          <!-- Loading Indicator -->
          <div *ngIf="loading()" class="py-12 text-center text-ink-muted">
            <span class="inline-block size-8 animate-spin rounded-full border-3 border-brand border-t-transparent mb-2"></span>
            <p class="font-medium text-ink">Cargando directorio de usuarios y roles...</p>
          </div>

          <!-- Error Alert -->
          <div *ngIf="error()" class="p-4 rounded-lg bg-danger-bg text-danger-text text-sm border border-border">
            <i class="bi bi-exclamation-triangle-fill mr-1"></i> {{ error() }}
          </div>

          <!-- Data Table -->
          <div *ngIf="!loading() && !error()" class="overflow-x-auto">
            <table class="w-full text-left border-collapse text-xs md:text-sm">
              <thead>
                <tr class="border-b border-border bg-surface-sunken text-ink-muted uppercase text-xs font-semibold tracking-wider">
                  <th class="py-3 px-4">Usuario / Persona</th>
                  <th class="py-3 px-4">Cargo / Posición</th>
                  <th class="py-3 px-4">Estructura Organizacional</th>
                  <th class="py-3 px-4">Grupo Ocupacional</th>
                  <th class="py-3 px-4 text-center">Rol en Sistema</th>
                  <th class="py-3 px-4 text-center">Estado</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border">
                @for (u of users(); track u.personnelId || u.userId) {
                  <tr class="hover:bg-surface-sunken/60 transition-colors">
                    <td class="py-3 px-4">
                      <div class="flex items-center gap-3">
                        <div class="size-9 rounded-full bg-brand-50 text-brand font-bold flex items-center justify-center text-xs shrink-0 border border-brand-200">
                          {{ getInitials(u) }}
                        </div>
                        <div>
                          <div class="font-semibold text-ink-heading">
                            {{ getFullName(u) }}
                          </div>
                          <div class="text-xs text-ink-muted font-mono">
                            {{ u.email }}
                          </div>
                          <div *ngIf="u.personnel?.identificationNumber" class="text-[11px] text-ink-subtle">
                            Cédula: {{ u.personnel?.identificationNumber }}
                          </div>
                        </div>
                      </div>
                    </td>

                    <td class="py-3 px-4 font-medium text-ink">
                      <div>{{ u.position?.name || 'Sin cargo registrado' }}</div>
                      <div *ngIf="u.position?.isManager" class="mt-0.5">
                        <span class="badge-warning !text-[10px]">Supervisorio / Manager</span>
                      </div>
                    </td>

                    <td class="py-3 px-4 text-xs text-ink-muted">
                      <div *ngIf="u.position?.organizationalStructure; else noOrg">
                        <span class="font-semibold text-ink">
                          {{ u.position?.organizationalStructure?.name }}
                        </span>
                        <span class="block text-[11px] font-mono text-ink-subtle">
                          Código: {{ u.position?.organizationalStructure?.code }}
                        </span>
                      </div>
                      <ng-template #noOrg>
                        <span class="text-ink-subtle">—</span>
                      </ng-template>
                    </td>

                    <td class="py-3 px-4 text-xs text-ink-muted">
                      {{ u.position?.occupationalGroup?.name || '—' }}
                    </td>

                    <td class="py-3 px-4 text-center">
                      <span
                        class="badge font-bold"
                        [ngClass]="getRoleBadgeClass(u.roleName)"
                      >
                        <i class="bi bi-shield-lock text-[10px]"></i>
                        {{ u.roleName || 'Sin Rol' }}
                      </span>
                    </td>

                    <td class="py-3 px-4 text-center">
                      <span
                        class="badge"
                        [ngClass]="u.personnel?.status === 'Active' ? 'badge-success' : 'badge-neutral'"
                      >
                        {{ u.personnel?.status || 'Active' }}
                      </span>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6" class="p-8 text-center text-ink-muted text-sm">
                      No se encontraron usuarios ni roles con los criterios especificados.
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Pagination Footer -->
          <div *ngIf="!loading() && !error()" class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-4 border-t border-border">
            <div class="flex items-center gap-2 text-xs text-ink-muted">
              <span>Mostrar por página:</span>
              <select
                class="field-control !h-8 !py-0 !px-2 !w-20 text-xs"
                [ngModel]="pageSize()"
                (ngModelChange)="cambiarPageSize($event)"
              >
                <option [value]="5">5</option>
                <option [value]="10">10</option>
                <option [value]="25">25</option>
                <option [value]="50">50</option>
              </select>
              <span>Total: {{ totalCount() }} usuarios</span>
            </div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                class="btn-secondary btn-sm"
                [disabled]="pageNumber() <= 1 || loading()"
                (click)="anteriorPagina()"
              >
                <i class="bi bi-chevron-left"></i> Anterior
              </button>
              <span class="text-xs font-semibold text-ink px-2">
                Página {{ pageNumber() }} de {{ totalPages() || 1 }}
              </span>
              <button
                type="button"
                class="btn-secondary btn-sm"
                [disabled]="pageNumber() >= totalPages() || loading()"
                (click)="siguientePagina()"
              >
                Siguiente <i class="bi bi-chevron-right"></i>
              </button>
            </div>
          </div>
        </div>
      </div>
    </app-page-shell>
  `,
})
export class UserAccessComponent implements OnInit {
  private readonly userPositionService = inject(UserPositionService);

  readonly users = signal<UserPositionDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);

  searchPosition = '';

  ngOnInit(): void {
    this.cargarUsuarios();
  }

  cargarUsuarios(): void {
    this.loading.set(true);
    this.error.set(null);

    this.userPositionService
      .getUserPositions(undefined, this.searchPosition, this.pageNumber(), this.pageSize())
      .subscribe({
        next: (res: UserPositionPagedResultDto) => {
          this.users.set(res.data || []);
          this.pageNumber.set(res.pageNumber || 1);
          this.pageSize.set(res.pageSize || 10);
          this.totalCount.set(res.totalCount || 0);
          this.totalPages.set(res.totalPages || 0);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Error al cargar los usuarios y roles del sistema.');
          this.loading.set(false);
        },
      });
  }

  buscar(): void {
    this.pageNumber.set(1);
    this.cargarUsuarios();
  }

  limpiarBusqueda(): void {
    this.searchPosition = '';
    this.buscar();
  }

  cambiarPageSize(size: number): void {
    this.pageSize.set(Number(size));
    this.pageNumber.set(1);
    this.cargarUsuarios();
  }

  anteriorPagina(): void {
    if (this.pageNumber() > 1) {
      this.pageNumber.update((p) => p - 1);
      this.cargarUsuarios();
    }
  }

  siguientePagina(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.pageNumber.update((p) => p + 1);
      this.cargarUsuarios();
    }
  }

  getFullName(user: UserPositionDto): string {
    if (user.personnel) {
      return `${user.personnel.firstName || ''} ${user.personnel.lastName || ''}`.trim();
    }
    return user.email || 'Usuario';
  }

  getInitials(user: UserPositionDto): string {
    const name = this.getFullName(user);
    const parts = name.split(' ').filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  getRoleBadgeClass(roleName?: string): string {
    if (!roleName) return 'badge-neutral';
    const lower = roleName.toLowerCase();
    if (lower.includes('super admin') || lower.includes('administrador')) {
      return 'badge-info';
    }
    if (lower.includes('local admin') || lower.includes('admin')) {
      return 'badge-success';
    }
    return 'badge-neutral';
  }
}
