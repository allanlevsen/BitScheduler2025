import { Routes } from '@angular/router';

import { EventCreatePageComponent } from './features/events/pages/event-create-page.component';
import { EventDeletePageComponent } from './features/events/pages/event-delete-page.component';
import { EventEditPageComponent } from './features/events/pages/event-edit-page.component';
import { EventListPageComponent } from './features/events/pages/event-list-page.component';
import { ResourceTypeCreatePageComponent } from './features/resource-types/pages/resource-type-create-page.component';
import { ResourceTypeDeletePageComponent } from './features/resource-types/pages/resource-type-delete-page.component';
import { ResourceTypeEditPageComponent } from './features/resource-types/pages/resource-type-edit-page.component';
import { ResourceTypeListPageComponent } from './features/resource-types/pages/resource-type-list-page.component';
import { ResourceCreatePageComponent } from './features/resources/pages/resource-create-page.component';
import { ResourceDeletePageComponent } from './features/resources/pages/resource-delete-page.component';
import { ResourceEditPageComponent } from './features/resources/pages/resource-edit-page.component';
import { ResourceListPageComponent } from './features/resources/pages/resource-list-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'events' },
  { path: 'events', component: EventListPageComponent },
  { path: 'events/new', component: EventCreatePageComponent },
  { path: 'events/:bitEventId/edit', component: EventEditPageComponent },
  { path: 'events/:bitEventId/delete', component: EventDeletePageComponent },
  { path: 'resources', component: ResourceListPageComponent },
  { path: 'resources/new', component: ResourceCreatePageComponent },
  { path: 'resources/:bitResourceId/edit', component: ResourceEditPageComponent },
  { path: 'resources/:bitResourceId/delete', component: ResourceDeletePageComponent },
  { path: 'resource-types', component: ResourceTypeListPageComponent },
  { path: 'resource-types/new', component: ResourceTypeCreatePageComponent },
  { path: 'resource-types/:bitResourceTypeId/edit', component: ResourceTypeEditPageComponent },
  { path: 'resource-types/:bitResourceTypeId/delete', component: ResourceTypeDeletePageComponent },
  { path: '**', redirectTo: 'events' }
];
