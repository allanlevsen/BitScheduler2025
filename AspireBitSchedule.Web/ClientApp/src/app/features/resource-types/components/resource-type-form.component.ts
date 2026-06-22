import { CommonModule } from '@angular/common';
import { Component, effect, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ResourceTypeListItem, ResourceTypeRequest } from '../models/resource-type.models';

@Component({
  selector: 'app-resource-type-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './resource-type-form.component.html',
  styleUrls: ['./resource-type-form.component.css']
})
export class ResourceTypeFormComponent {
  public readonly title = input('Resource Type');
  public readonly submitLabel = input('Save Resource Type');
  public readonly initialValue = input<ResourceTypeListItem | null>(null);
  public readonly saving = input(false);
  public readonly errorMessage = input<string | null>(null);

  public readonly save = output<ResourceTypeRequest>();
  public readonly cancel = output<void>();

  private readonly formBuilder = new FormBuilder();

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', Validators.required]
  });

  public constructor() {
    effect(() => {
      const resourceType = this.initialValue();

      this.form.reset({
        name: resourceType?.name ?? ''
      });
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      name: value.name.trim()
    });
  }
}
