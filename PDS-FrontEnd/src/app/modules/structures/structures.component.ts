import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import {
  StructureModalComponent,
  StructureModalConfig,
} from '../../components/ui/modals/structure-modal.component/structure-modal.component';
import { StructurePanelComponent } from '../../components/ui/structure-panel/structure-panel.component';
import { StructureImportModalComponent } from '../../components/ui/modals/structure-import-modal/structure-import-modal.component';
import { StructureUpsert, StructureVisualizer } from '../../models/structure.model';
import { StructureService } from '../../services/structure/structure.service';
import { StructureVersionService } from '../../services/structure-version/structure-version.service';
import { LayoutService } from '@public-intelligence/ui-core';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../../services/notification/notification.service';
import { downloadTemplate, toTemplate } from '../../utils/structure-template.util';

@Component({
  selector: 'app-structure',
  imports: [
    ConfirmationModalComponent,
    StructureModalComponent,
    StructurePanelComponent,
    StructureImportModalComponent,
  ],
  templateUrl: './structures.component.html',
})
export class StructureComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  @ViewChild(StructureImportModalComponent)
  importModal!: StructureImportModalComponent;

  isLoading = signal<boolean>(false);
  structuresData = signal<StructureVisualizer[]>([]);
  protected isModalOpen = signal(false);
  structureModalConfig = signal<StructureModalConfig>({ isEdit: false });

  // La estructura solo es editable cuando existe un borrador de versión en curso.
  protected hasDraft = signal(false);

  notification = inject(NotificationService);
  structureService = inject(StructureService);
  private versionService = inject(StructureVersionService);
  private layoutService = inject(LayoutService);

  ngOnInit(currentId?: string | null) {
    this.layoutService.setPageTitle('Estructuras Organizacionales');
    this.layoutService.breadcrumbs.set([
      { label: 'Estructuras Organizacionales', url: '/structures' },
    ]);
    this.onGetStructures(currentId);
    this.loadDraftState();
  }

  /** Determina si hay un borrador en curso para habilitar/bloquear la edición. */
  private loadDraftState = async (): Promise<void> => {
    try {
      const drafts = await firstValueFrom(this.versionService.list('Draft'));
      this.hasDraft.set(drafts.length > 0);
    } catch {
      this.hasDraft.set(false);
    }
  };

  /** Guarda de edición: avisa al usuario si intenta modificar sin un borrador. */
  private ensureDraft(): boolean {
    if (this.hasDraft()) return true;
    this.notification.warning('Debe crear un borrador antes de modificar la estructura.');
    return false;
  }

  /** Crea un borrador de versión para habilitar la edición de la estructura. */
  protected createDraft = async (): Promise<void> => {
    try {
      await firstValueFrom(this.versionService.createDraft({ name: 'Nuevo borrador' }));
      this.hasDraft.set(true);
      this.notification.success('Borrador creado. Ya puede modificar la estructura.');
    } catch (e) {
      this.notification.fromHttpError(e, 'No se pudo crear el borrador. ¿Ya existe una versión en curso?');
    }
  };

  /**
   * Obtiene las estructuras y las almacena en el estado.
   * @param currentId Identificador de la estructura a mostrar.
   */
  onGetStructures = async (currentId?: string | null) => {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(this.structureService.getAll());
      if (currentId) {
        return this.structuresData.set(this.onShowDependencyById(currentId, result.items));
      }
      this.structuresData.set(result.items);
    } catch (error) {
      this.notification.onToast({
        message: 'An unknown error occurred',
        status: 'error',
      });
    } finally {
      this.isLoading.set(false);
    }
  };

  /**
   * Elimina la dependencia
   * @param id Identificador de la dependencia.
   */
  onDeleteStructure = async (id: string) => {
    await firstValueFrom(this.structureService.delete(id))
      .then(() => {
        this.notification.onToast({
          message: 'Dependencia eliminada con éxito',
          status: 'success',
        });
        this.ngOnInit();
        this.confirmationModal.closeModal();
      })
      .catch((error) => {
        this.notification.onToast({
          message: error?.title || 'An unknown error occurred',
          status: 'error',
        });
      });
  };

  /**
   * Función que muestra las dependencias de una estructura.
   * @param structureId
   * @param divisionLevel
   */
  onShowDependencies = (structureId: string, divisionLevel: number) => {
    // Función recursiva que recorre la lista de nodos y decrementa el nivel
    const toggleAtLevel = (nodes: StructureVisualizer[], level: number): StructureVisualizer[] => {
      return nodes.map((node) => {
        // Si estamos en el nivel deseado y el id coincide, alternamos el valor
        if (level === 0 && node.id === structureId) {
          return { ...node, showDependencies: !node.showDependencies };
        }
        // Si existen dependencias y aún no hemos alcanzado el nivel deseado, se recurre
        if (node.dependencies && level > 0) {
          return {
            ...node,
            dependencies: toggleAtLevel(node.dependencies, level - 1),
          };
        }
        return node;
      });
    };

    // Se aplica la función recursiva sobre la estructura completa
    const updatedStructures = toggleAtLevel(this.structuresData(), divisionLevel);

    return this.structuresData.set(updatedStructures);
  };

  /**
   * Función que muestra las dependencias de una estructura en específico.
   * @param structureId
   * @param divisionLevel
   */

  onShowDependencyById = (
    structureId: string,
    response: StructureVisualizer[],
  ): StructureVisualizer[] => {
    // Función recursiva que actualiza los nodos.
    // Devuelve un tuple: [nodosActualizados, flag indicando si se encontró el nodo en esta rama]
    const updateNodes = (nodes: StructureVisualizer[]): [StructureVisualizer[], boolean] => {
      let foundInBranch = false;
      const updatedNodes = nodes.map((node) => {
        // Procesar recursivamente las dependencias, si existen.
        let updatedDependencies: StructureVisualizer[] | undefined = undefined;
        let foundInChildren = false;
        if (node.dependencies && node.dependencies.length > 0) {
          const [updatedDeps, childFound] = updateNodes(node.dependencies);
          updatedDependencies = updatedDeps;
          foundInChildren = childFound;
        }

        // Si el nodo es el que buscamos, o si en sus hijos se encontró el nodo,
        // se marca el nodo como parte de la ruta y se deja abierto.
        if (node.id === structureId || foundInChildren) {
          foundInBranch = true;
          return {
            ...node,
            showDependencies: true,
            dependencies: updatedDependencies || node.dependencies,
          };
        } else {
          // Si el nodo no forma parte de la ruta, se cierra.
          return {
            ...node,
            showDependencies: node.showDependencies,
            dependencies: updatedDependencies || node.dependencies,
          };
        }
      });
      return [updatedNodes, foundInBranch];
    };

    const [updatedResponse] = updateNodes(response);
    return updatedResponse;
  };

  /**
   * Muestra el modal para crear una dependencia.
   * @param parentId Identificador de la dependencia padre.
   * @param parentName Nombre de la dependencia padre.
   * @param parentCode Código de la dependencia padre.
   */

  onShowCreateModal = ({ id, name, code, areaTypeId }: { id?: string; name?: string; code?: string; areaTypeId?: string }) => {
    if (!this.ensureDraft()) return;
    this.isModalOpen.set(true);
    this.structureModalConfig.set({
      parentId: id,
      parentName: name,
      parentCode: code,
      parentAreaTypeId: areaTypeId,
      isEdit: false,
    });
  };

  onShowEditModal = ({ id, name, code, parentId, parentName, areaTypeId }: StructureUpsert) => {
    if (!this.ensureDraft()) return;
    this.isModalOpen.set(true);
    this.structureModalConfig.set({
      id,
      name,
      code,
      parentId: parentId || null,
      parentName,
      areaTypeId,
      isEdit: true,
    });
  };

  protected onModalClosed = (): void => {
    this.isModalOpen.set(false);
    this.onGetStructures(this.structureModalConfig().id || null);
  };

  /**
   * Muestra el modal de confirmación para eliminar una dependencia.
   * @param id Identificador de la dependencia.
   */
  onShowDeleteConfirmationModal = ({
    id,
    name,
    code,
  }: {
    id: string;
    name: string;
    code: string;
  }) => {
    if (!this.ensureDraft()) return;
    const structureName = name;
    const structureCode = code;

    this.confirmationModal.openModal({
      title: 'Eliminar dependencia',
      name: `${structureName} con código ${structureCode}`,
      isDelete: true,
      paragraph: `Está seguro que desea eliminar la dependencia`,
      warning:
        'Si elimina esta dependencia, todas las depencias añadidas a este serán eliminadas. Esta acción no puede deshacerse.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeleteStructure(id);
      },
    });
  };

  /**
   * Retorna la estructura dentro del arreglo.
   * @param id Identificador de la estructura.
   */
  onGetStructureInArray(id: string) {
    const structure = this.structuresData().filter((structure) => structure.id === id);
    return structure[0];
  }

  /** Exporta la estructura actual a un archivo JSON portable (plantilla). */
  protected onExport = (): void => {
    const structures = this.structuresData();
    if (structures.length === 0) {
      this.notification.warning('No hay estructura para exportar.');
      return;
    }
    downloadTemplate(toTemplate(structures), 'estructura-organizacional.json');
  };

  /** Abre el modal para importar una estructura desde un JSON. */
  protected onOpenImport = (): void => {
    this.importModal.open();
  };

  /** Recarga la estructura y el estado del borrador tras una importación exitosa. */
  protected onImported = (): void => {
    this.onGetStructures();
    this.loadDraftState();
  };
}
