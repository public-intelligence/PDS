import { NgClass } from '@angular/common';
import { Component, Input, signal } from '@angular/core';
import { StructureUpsert, StructureVisualizer } from '../../../models/structure.model';

@Component({
  selector: 'app-structure-panel',
  imports: [NgClass, StructurePanelComponent],
  templateUrl: './structure-panel.component.html',
})
export class StructurePanelComponent {
  @Input() id = '';
  @Input() parentId: string | undefined = '';
  @Input() parentName: string | undefined = '';
  @Input() name = '';
  @Input() code = '00';
  @Input() areaTypeId = '';
  @Input() areaName = '';
  @Input() categoryName = '';
  @Input() divisionLevel = 0;
  @Input() dependencies!: StructureVisualizer[];
  @Input() showDependencies: boolean | undefined = false;
  @Input() onShowDependencies!: (id: string, divisionLevel: number) => void;
  @Input() onShowCreateModal!: ({
    id,
    name,
    code,
    areaTypeId,
  }: {
    id: string;
    name: string;
    code: string;
    areaTypeId: string;
  }) => void;
  @Input() onShowDeleteConfirmationModal!: ({
    id,
    name,
    code,
  }: {
    id: string;
    name: string;
    code: string;
  }) => void;
  @Input() onShowEditModal!: ({
    id,
    parentId,
    parentName,
    name,
    code,
    areaTypeId,
  }: StructureUpsert) => void;

  showActions = signal<boolean>(false);

  onToggleActions = (bool: boolean) => {
    this.showActions.set(bool);
  };

  onAnimationEnd(event: AnimationEvent) {
    const element = event.target as HTMLElement;
    element.classList.remove('slide-in-top');
  }
}
