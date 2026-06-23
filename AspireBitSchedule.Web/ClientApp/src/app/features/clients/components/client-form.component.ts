import { CommonModule } from '@angular/common';
import { Component, effect, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ClientListItem, ClientRequest } from '../models/client.models';

@Component({
  selector: 'app-client-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './client-form.component.html',
  styleUrls: ['./client-form.component.css']
})
export class ClientFormComponent {
  public readonly title = input('Client');
  public readonly submitLabel = input('Save Client');
  public readonly initialValue = input<ClientListItem | null>(null);
  public readonly saving = input(false);
  public readonly errorMessage = input<string | null>(null);

  public readonly save = output<ClientRequest>();
  public readonly cancel = output<void>();

  private readonly formBuilder = new FormBuilder();

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', Validators.required]
  });

  public constructor() {
    effect(() => {
      const client = this.initialValue();

      this.form.reset({
        name: client?.name ?? ''
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
