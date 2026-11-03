import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageShellComponent } from '@public-intelligence/ui-core';
import { PlanningStoreService } from '../../data/planning-store.service';

interface TareaCritica {
  titulo: string;
  detalle: string;
  ruta: string;
  tono: 'error' | 'warning' | 'info';
}

interface SemaforoModulo {
  modulo: string;
  estado: 'ok' | 'atencion' | 'pendiente';
  detalle: string;
  ruta: string;
}

/**
 * Inicio del módulo de planificación: orientación inmediata según la persona
 * activa — qué hacer ahora, estado del ciclo y actividad reciente.
 */
@Component({
  selector: 'app-planning-inicio',
  imports: [PageShellComponent, RouterLink],
  template: `
    <app-page-shell
      [title]="saludo()"
      [description]="'Período activo: ' + store.cicloActivo().nombre + ' · Formulación abierta hasta el 14/08/2026'"
    >
      <!-- Tareas críticas -->
      <div class="grid grid-cols-1 gap-4 lg:grid-cols-3">
        @for (t of tareas(); track t.titulo) {
          <a
            [routerLink]="t.ruta"
            class="card block p-4 !no-underline transition-shadow hover:shadow-md"
          >
            <span
              class="mb-2 inline-block"
              [class]="
                t.tono === 'error' ? 'badge-error' : t.tono === 'warning' ? 'badge-warning' : 'badge-info'
              "
              >Requiere acción</span
            >
            <p class="!text-base font-semibold text-ink">{{ t.titulo }}</p>
            <p class="mt-1 !text-sm text-ink-muted">{{ t.detalle }}</p>
          </a>
        } @empty {
          <div class="card p-4 lg:col-span-3">
            <p class="!text-sm text-success-text">No tiene tareas críticas pendientes en este momento.</p>
          </div>
        }
      </div>

      <div class="mt-4 grid grid-cols-1 gap-4 xl:grid-cols-3">
        <!-- Estado del ciclo -->
        <div class="card p-4 xl:col-span-2">
          <h2 class="mb-3 !font-poppins !text-base font-semibold text-ink">
            Estado del ciclo {{ store.cicloActivo().nombre }}
          </h2>
          <ul class="grid grid-cols-1 gap-2 sm:grid-cols-2">
            @for (s of semaforo(); track s.modulo) {
              <li>
                <a
                  [routerLink]="s.ruta"
                  class="flex items-center gap-3 rounded-lg border border-border px-3 py-2.5 !no-underline transition-colors hover:bg-surface"
                >
                  <span
                    class="size-2.5 shrink-0 rounded-full"
                    [class]="
                      s.estado === 'ok' ? 'bg-success' : s.estado === 'atencion' ? 'bg-warning' : 'bg-border-strong'
                    "
                  ></span>
                  <span class="min-w-0">
                    <span class="block !text-sm font-medium text-ink">{{ s.modulo }}</span>
                    <span class="block truncate !text-xs text-ink-muted">{{ s.detalle }}</span>
                  </span>
                </a>
              </li>
            }
          </ul>

          <h2 class="mt-5 mb-2 !font-poppins !text-base font-semibold text-ink">Accesos rápidos</h2>
          <div class="flex flex-wrap gap-2">
            <a routerLink="/plan-anual" class="btn-primary !no-underline">Ir a plan anual</a>
            <a routerLink="/ciclos/nuevo" class="btn-secondary !no-underline">Crear ciclo</a>
            <a routerLink="/requerimientos" class="btn-secondary !no-underline">Revisar requerimientos</a>
            <a routerLink="/seguimiento" class="btn-secondary !no-underline">Reportar avances</a>
          </div>
        </div>

        <!-- Actividad reciente -->
        <div class="card p-4">
          <h2 class="mb-3 !font-poppins !text-base font-semibold text-ink">Actividad reciente</h2>
          <ul class="flex flex-col gap-3">
            @for (ev of store.actividad().slice(0, 6); track ev.id) {
              <li class="flex gap-3">
                <span class="mt-1.5 size-1.5 shrink-0 rounded-full bg-brand-300"></span>
                <span class="min-w-0">
                  <span class="block !text-sm text-ink">
                    <strong class="font-medium">{{ ev.persona }}</strong> · {{ ev.accion }}
                  </span>
                  <span class="block !text-xs text-ink-muted">{{ ev.detalle }}</span>
                  <span class="block !text-[11px] text-ink-subtle">{{ ev.fecha }} · {{ ev.modulo }}</span>
                </span>
              </li>
            }
          </ul>
        </div>
      </div>
    </app-page-shell>
  `,
})
export class PlanningInicioComponent {
  protected readonly store = inject(PlanningStoreService);

  protected readonly saludo = computed(() => {
    const nombre = this.store.persona().nombre.split(' ')[0];
    return `Hola, ${nombre}`;
  });

  protected readonly tareas = computed<TareaCritica[]>(() => {
    const persona = this.store.personaId();
    const planes = this.store.planes();
    const reqs = this.store.requerimientos();
    const areaId = this.store.persona().areaId;

    switch (persona) {
      case 'planificacion': {
        const listos = planes.filter((p) => p.estado === 'Listo para revisión').length;
        const sinIniciar = planes.filter((p) => p.estado === 'Sin iniciar').length;
        return [
          {
            titulo: `${listos} plan(es) listos para revisión`,
            detalle: 'Revise y consolide los planes sometidos por las áreas.',
            ruta: '/plan-anual',
            tono: 'warning',
          },
          {
            titulo: `${sinIniciar} áreas sin iniciar su plan`,
            detalle: 'La formulación cierra el 14/08/2026. Considere enviar recordatorio.',
            ruta: '/plan-anual',
            tono: 'error',
          },
          {
            titulo: 'Preparar consolidación institucional',
            detalle: 'Valide duplicidades y consistencia entre áreas.',
            ruta: '/consolidacion',
            tono: 'info',
          },
        ];
      }
      case 'sustantiva': {
        const plan = planes.find((p) => p.areaId === areaId);
        const incompletas = plan ? this.store.planIssueCount(plan) : 0;
        return [
          {
            titulo: 'Completar el plan anual de su dirección',
            detalle: `${incompletas} fila(s) con información faltante. Fecha límite: 14/08/2026.`,
            ruta: plan ? '/plan-anual/' + plan.id : '/plan-anual',
            tono: incompletas ? 'error' : 'info',
          },
          {
            titulo: 'Requerimiento devuelto por Administrativa',
            detalle: 'RQ-2027-010 requiere especificación técnica.',
            ruta: '/requerimientos',
            tono: 'warning',
          },
        ];
      }
      case 'transversal': {
        const bandeja = reqs.filter(
          (r) => this.store.consolidadoraDeCategoria(r.categoriaId)?.id === areaId && r.estado === 'Nuevo',
        ).length;
        const enRevision = reqs.filter(
          (r) => this.store.consolidadoraDeCategoria(r.categoriaId)?.id === areaId && r.estado === 'En revisión',
        ).length;
        return [
          {
            titulo: `${bandeja} requerimiento(s) nuevos en su bandeja`,
            detalle: 'Clasifique, apruebe o devuelva las solicitudes enrutadas a su área.',
            ruta: '/requerimientos',
            tono: 'warning',
          },
          {
            titulo: `${enRevision} requerimiento(s) en revisión`,
            detalle: 'Hay solicitudes esperando información de las áreas de origen.',
            ruta: '/requerimientos',
            tono: 'info',
          },
        ];
      }
      case 'presupuesto': {
        return [
          {
            titulo: 'Rubro con exceso de techo presupuestario',
            detalle: 'El rubro «Combustible» supera su techo institucional. Revise techos y consistencia.',
            ruta: '/consolidacion',
            tono: 'error',
          },
          {
            titulo: 'Validación financiera del consolidado',
            detalle:
              this.store.aprobacionFinanciera() === 'aprobada'
                ? 'Validación completada. El consolidado pasó a aprobación directiva.'
                : 'Pendiente de su validación para pasar a aprobación directiva.',
            ruta: '/consolidacion',
            tono: 'warning',
          },
        ];
      }
      case 'mae': {
        const poaAprobado = !!this.store.selloDe('POA');
        const paccAprobado = !!this.store.selloDe('PACC');
        return [
          {
            titulo:
              poaAprobado && paccAprobado
                ? 'POA y PACC 2027 aprobados'
                : 'Expediente de aprobación en preparación',
            detalle:
              poaAprobado && paccAprobado
                ? 'Ambos artefactos fueron aprobados y sellados.'
                : poaAprobado
                  ? 'POA aprobado. Falta aprobar el PACC.'
                  : 'Cuando Presupuesto valide el consolidado, podrá aprobar el POA y el PACC.',
            ruta: '/aprobacion',
            tono: 'info',
          },
        ];
      }
    }
  });

  protected readonly semaforo = computed<SemaforoModulo[]>(() => {
    const planes = this.store.planes();
    const sometidos = planes.filter((p) =>
      ['Listo para revisión', 'Aprobado'].includes(p.estado),
    ).length;
    const nuevos = this.store.requerimientos().filter((r) => r.estado === 'Nuevo').length;
    const pendCorte = this.store.seguimiento().filter((s) => s.estado === 'Pendiente').length;
    const enAjustes = planes.filter((p) => p.estado === 'En ajustes').length;
    return [
      {
        modulo: 'Estructura organizativa',
        estado: 'ok',
        detalle: 'Versión vigente publicada (sin cambios en curso)',
        ruta: '/structures',
      },
      {
        modulo: 'Plan anual',
        estado: sometidos === planes.length ? 'ok' : 'atencion',
        detalle: `${sometidos} de ${planes.length} planes sometidos`,
        ruta: '/plan-anual',
      },
      {
        modulo: 'Requerimientos transversales',
        estado: nuevos ? 'atencion' : 'ok',
        detalle: `${nuevos} sin clasificar`,
        ruta: '/requerimientos',
      },
      {
        modulo: 'Consolidación',
        estado: this.store.aprobacionFinanciera() === 'aprobada' ? 'ok' : 'pendiente',
        detalle:
          this.store.aprobacionFinanciera() === 'aprobada'
            ? 'Validada por Presupuesto'
            : 'Aún no iniciada',
        ruta: '/consolidacion',
      },
      {
        modulo: 'Ajustes y priorización',
        estado: enAjustes ? 'atencion' : 'ok',
        detalle: enAjustes ? `${enAjustes} plan(es) en ajustes por las áreas` : 'Sin planes en ajustes',
        ruta: '/consolidacion',
      },
      {
        modulo: 'Seguimiento (ciclo 2026)',
        estado: pendCorte ? 'atencion' : 'ok',
        detalle: `${pendCorte} registros pendientes del corte de junio`,
        ruta: '/seguimiento',
      },
    ];
  });
}
