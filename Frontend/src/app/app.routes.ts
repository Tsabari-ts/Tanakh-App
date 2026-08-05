import { Routes } from '@angular/router';
import { environment } from '../environments/environment';

export const routes: Routes = [
  { path: "", redirectTo: "entrance", pathMatch: "full" },
  {
    // Hidden admin panel - deliberately not in the app's navigation, not
    // localized, and excluded from the sitemap/robots. See
    // Frontend/src/assets/_headers for the noindex header on this prefix.
    path: environment.adminRoutePath,
    loadChildren: () => import('./admin/admin.routes').then(m => m.adminRoutes)
  },
  {
    path: "entrance",
    loadComponent: () => import('./components/entrance/entrance.component').then(m => m.EntranceComponent),
    title: $localize`:@@route.entrance.title:תנ"ך`
  },
  {
    path: "home",
    loadComponent: () => import('./components/home/home.component').then(m => m.HomeComponent),
    title: $localize`:@@route.home.title:תנ"ך`
  },
  {
    path: "settings",
    loadComponent: () => import('./components/settings/settings.component').then(m => m.SettingsComponent),
    title: $localize`:@@route.settings.title:הגדרות`
  },
  {
    path: "books/:section",
    loadComponent: () => import('./components/booklist/booklist.component').then(m => m.BooklistComponent),
    title: $localize`:@@route.booklist.title:ספרי התנ"ך`
  },
  {
    path: "books/:section/:book",
    loadComponent: () => import('./components/chapterlist/chapterlist.component').then(m => m.ChapterlistComponent),
    title: $localize`:@@route.chapterlist.title:פרקים`
  },
  {
    path: "books/:section/:book/:chapterNumber/:keepReading",
    loadComponent: () => import('./components/chapter/chapter.component').then(m => m.ChapterComponent),
    title: $localize`:@@route.chapter.title:תנ"ך`
  },
  { path: "*", redirectTo: "home" }
];
