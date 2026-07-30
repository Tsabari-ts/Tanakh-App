import { provideZoneChangeDetection, importProvidersFrom } from "@angular/core";
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';


import { WelcomeModalComponent } from "./app/components/welcome-modal/welcome-modal.component";
import { SubscribeComponent } from "./app/components/subscribe/subscribe.component";
import { ReadPermissionComponent } from "./app/components/read-permission/read-permission.component";
import { provideHttpClient, withXhr, withInterceptorsFromDi } from "@angular/common/http";
import { BrowserModule, bootstrapApplication } from "@angular/platform-browser";
import { AppRoutingModule } from "./app/app-routing.module";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { MatDialogModule } from "@angular/material/dialog";
import { MatIconModule } from "@angular/material/icon";
import { FormsModule } from "@angular/forms";
import { provideRouter } from "@angular/router";
import { EntranceComponent } from "./app/components/entrance/entrance.component";
import { HomeComponent } from "./app/components/home/home.component";
import { SettingsComponent } from "./app/components/settings/settings.component";
import { BooklistComponent } from "./app/components/booklist/booklist.component";
import { ChapterlistComponent } from "./app/components/chapterlist/chapterlist.component";
import { ChapterComponent } from "./app/components/chapter/chapter.component";
import { ServiceWorkerModule } from "@angular/service-worker";
import { environment } from "./environments/environment";
import { AppComponent } from "./app/app.component";


bootstrapApplication(AppComponent, {
    providers: [
        importProvidersFrom(BrowserModule, AppRoutingModule, BrowserAnimationsModule, MatDialogModule, MatIconModule, FormsModule, ServiceWorkerModule.register('ngsw-worker.js', {
            enabled: environment.enableServiceWorker,
            registrationStrategy: 'registerWhenStable:30000'
        })),
        WelcomeModalComponent, SubscribeComponent, ReadPermissionComponent, provideHttpClient(withXhr(), withInterceptorsFromDi()),
        provideRouter([
            { path: "", redirectTo: "entrance", pathMatch: "full" },
            { path: "entrance", component: EntranceComponent },
            { path: "home", component: HomeComponent },
            { path: "settings", component: SettingsComponent },
            { path: "books/:section", component: BooklistComponent },
            { path: "books/:section/:book", component: ChapterlistComponent },
            { path: "books/:section/:book/:chapterNumber/:keepReading", component: ChapterComponent },
            { path: "*", redirectTo: "home" }
        ])
    ]
})
  .catch(err => console.error(err));
