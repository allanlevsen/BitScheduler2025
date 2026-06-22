import { Routes } from '@angular/router';

import { EventCreatePageComponent } from './features/events/pages/event-create-page.component';
import { EventDeletePageComponent } from './features/events/pages/event-delete-page.component';
import { EventEditPageComponent } from './features/events/pages/event-edit-page.component';
import { EventListPageComponent } from './features/events/pages/event-list-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'events' },
  { path: 'events', component: EventListPageComponent },
  { path: 'events/new', component: EventCreatePageComponent },
  { path: 'events/:bitEventId/edit', component: EventEditPageComponent },
  { path: 'events/:bitEventId/delete', component: EventDeletePageComponent },
  { path: '**', redirectTo: 'events' }
];
