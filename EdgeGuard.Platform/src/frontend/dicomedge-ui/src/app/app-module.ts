import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FaIconLibrary, FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faHome, faUser, faCog, faSearch, faBars,
  faPlus, faEdit, faTrash, faCheck, faTimes,
  faChevronLeft, faChevronRight, faSpinner,
  faExclamationTriangle, faInfoCircle,
} from '@fortawesome/free-solid-svg-icons';
import {
  faBell as farBell,
  faCircle as farCircle,
} from '@fortawesome/free-regular-svg-icons';

import { AppRoutingModule } from './app-routing-module';
import { MaterialModule } from './material/material.module';
import { App } from './app';

@NgModule({
  declarations: [
    App
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    MaterialModule,
    FontAwesomeModule,
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
  ],
  bootstrap: [App]
})
export class AppModule {
  constructor(library: FaIconLibrary) {
    library.addIcons(
      faHome, faUser, faCog, faSearch, faBars,
      faPlus, faEdit, faTrash, faCheck, faTimes,
      faChevronLeft, faChevronRight, faSpinner,
      faExclamationTriangle, faInfoCircle,
      farBell, farCircle,
    );
  }
}
