import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UnidadesConfigComponent } from './unidades-config.component';
import { PlanningConfigService, PlanningAreaConfigDto } from '../../../../services/planning/planning-config.service';
import { NotificationService } from '../../../../services/notification/notification.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('UnidadesConfigComponent', () => {
  let component: UnidadesConfigComponent;
  let fixture: ComponentFixture<UnidadesConfigComponent>;
  let configService: PlanningConfigService;

  const mockConfig: PlanningAreaConfigDto = {
    maxLevel: 2,
    description: 'Unidades hasta nivel 2',
    qualifyingAreas: [
      { id: 'area-0', code: '00', name: 'Despacho', parentId: null, parentName: null, depthLevel: 0, qualifiesForPlanning: true },
      { id: 'area-1', code: '01', name: 'Viceministerio', parentId: 'area-0', parentName: 'Despacho', depthLevel: 1, qualifiesForPlanning: true },
      { id: 'area-2', code: '02', name: 'Dirección', parentId: 'area-1', parentName: 'Viceministerio', depthLevel: 2, qualifiesForPlanning: true },
      { id: 'area-3', code: '03', name: 'Departamento', parentId: 'area-2', parentName: 'Dirección', depthLevel: 3, qualifiesForPlanning: false },
    ],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UnidadesConfigComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: NotificationService,
          useValue: {
            success: vi.fn(),
            error: vi.fn(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UnidadesConfigComponent);
    component = fixture.componentInstance;
    configService = TestBed.inject(PlanningConfigService);

    vi.spyOn(configService, 'cargarConfiguracion').mockResolvedValue(mockConfig);
    vi.spyOn(configService, 'actualizarGuardrailNivel').mockResolvedValue(mockConfig);

    await component.ngOnInit();
    fixture.detectChanges();
  });

  it('should initialize with level 2 and correct selected areas', () => {
    expect(component).toBeTruthy();
    expect(component['selectedLevel']()).toBe(2);
    expect(component['selectedAreaIds']().has('area-0')).toBe(true);
    expect(component['selectedAreaIds']().has('area-1')).toBe(true);
    expect(component['selectedAreaIds']().has('area-2')).toBe(true);
    expect(component['selectedAreaIds']().has('area-3')).toBe(false);
  });

  it('should evaluate to level 1 when area-2 is unchecked and selection matches level 1 depth rule', () => {
    component['configService'].config.set(mockConfig);
    component['toggleAreaSelection']('area-2', { target: { checked: false } } as unknown as Event);
    expect(component['selectedLevel']()).toBe(1);
  });

  it('should select all units when level 99 (Sin Restricción) is clicked', () => {
    component['configService'].config.set(mockConfig);
    component['seleccionarNivel'](99);
    expect(component['selectedLevel']()).toBe(99);
    expect(component['selectedAreaIds']().size).toBe(4);
  });
});
