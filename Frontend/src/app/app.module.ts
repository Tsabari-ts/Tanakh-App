import { NgModule, isDevMode } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { RouterModule } from '@angular/router';
import { EntranceComponent } from './components/entrance/entrance.component';
import { HomeComponent } from './components/home/home.component';
import { HttpClientModule } from '@angular/common/http';
import { ServiceWorkerModule } from '@angular/service-worker';
import { SettingsComponent } from './components/settings/settings.component';
import { BooklistComponent } from './components/booklist/booklist.component';
import { ChapterlistComponent } from './components/chapterlist/chapterlist.component';
import { ChapterComponent } from './components/chapter/chapter.component';
import { WelcomeModalComponent } from './components/welcome-modal/welcome-modal.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { SubscribeComponent } from './components/subscribe/subscribe.component';
import { FormsModule } from '@angular/forms';
import { ScrollToTopButtonComponent } from './components/scroll-to-top-button/scroll-to-top-button.component';
import { ReadPermissionComponent } from './components/read-permission/read-permission.component';

@NgModule({
  declarations: [
    AppComponent,
    EntranceComponent,
    HomeComponent,
    SettingsComponent,
    BooklistComponent,
    ChapterlistComponent,
    ChapterComponent,
    WelcomeModalComponent,
    SubscribeComponent,
    ScrollToTopButtonComponent,
    ReadPermissionComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatDialogModule,
    MatIconModule,
    FormsModule,
    RouterModule.forRoot([
      {path:"",redirectTo:"entrance",pathMatch:"full"},
      {path:"entrance", component:EntranceComponent},
      {path:"home",component:HomeComponent},
      {path:"settings",component:SettingsComponent},
      {path:"books/:section",component:BooklistComponent},
      {path:"books/:section/:book",component:ChapterlistComponent},
      {path:"books/:section/:book/:chapterNumber/:keepReading",component:ChapterComponent},
      {path:"*",redirectTo:"home"}
    ]),
    ServiceWorkerModule.register('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ],
  providers: [WelcomeModalComponent, SubscribeComponent, ReadPermissionComponent],
  bootstrap: [AppComponent]
})
export class AppModule { }
