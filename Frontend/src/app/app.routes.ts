import { Routes } from '@angular/router';
import { EntranceComponent } from './components/entrance/entrance.component';
import { HomeComponent } from './components/home/home.component';
import { SettingsComponent } from './components/settings/settings.component';
import { BooklistComponent } from './components/booklist/booklist.component';
import { ChapterlistComponent } from './components/chapterlist/chapterlist.component';
import { ChapterComponent } from './components/chapter/chapter.component';

export const routes: Routes = [
  { path: "", redirectTo: "entrance", pathMatch: "full" },
  { path: "entrance", component: EntranceComponent },
  { path: "home", component: HomeComponent },
  { path: "settings", component: SettingsComponent },
  { path: "books/:section", component: BooklistComponent },
  { path: "books/:section/:book", component: ChapterlistComponent },
  { path: "books/:section/:book/:chapterNumber/:keepReading", component: ChapterComponent },
  { path: "*", redirectTo: "home" }
];
