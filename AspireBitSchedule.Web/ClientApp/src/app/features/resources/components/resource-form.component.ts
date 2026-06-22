import { CommonModule } from '@angular/common';
import { Component, effect, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ResourceListItem, ResourceRequest, ResourceTypeListItem } from '../models/resource.models';

@Component({
  selector: 'app-resource-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './resource-form.component.html',
  styleUrls: ['./resource-form.component.css']
})
export class ResourceFormComponent {
  public readonly title = input('Resource');
  public readonly submitLabel = input('Save Resource');
  public readonly resourceTypes = input<ResourceTypeListItem[]>([]);
  public readonly initialValue = input<ResourceListItem | null>(null);
  public readonly saving = input(false);
  public readonly errorMessage = input<string | null>(null);

  public readonly save = output<ResourceRequest>();
  public readonly cancel = output<void>();

  private readonly formBuilder = new FormBuilder();

  protected readonly form = this.formBuilder.nonNullable.group({
    bitResourceTypeId: [0, [Validators.required, Validators.min(1)]],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    emailAddress: ['', [Validators.required, Validators.email]]
  });

  public constructor() {
    effect(() => {
      const resource = this.initialValue();

      if (!resource) {
        this.form.reset({
          bitResourceTypeId: 0,
          firstName: '',
          lastName: '',
          emailAddress: ''
        });

        return;
      }

      this.form.reset({
        bitResourceTypeId: resource.bitResourceTypeId,
        firstName: resource.firstName,
        lastName: resource.lastName,
        emailAddress: resource.emailAddress
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
      bitResourceTypeId: value.bitResourceTypeId,
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      emailAddress: value.emailAddress.trim()
    });
  }
}
