import { Component, OnInit, ElementRef, Renderer2, ChangeDetectionStrategy, computed } from '@angular/core';
import { PwaInstallService } from '../../services/pwa-install.service';
import { AppComponent } from '../../app.component';
import { NgClass } from '@angular/common';
import { WHATSAPP_CONTACT_URL, CONTACT_EMAIL } from '../../shared/contact-links';
import { ThemeService } from '../../services/theme.service';
import { ReaderPrefsService, ReaderFont } from '../../services/reader-prefs.service';
import { SubscribeComponent } from '../subscribe/subscribe.component';

@Component({
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgClass, SubscribeComponent]
})

export class SettingsComponent implements OnInit {
  constructor(private renderer: Renderer2,
              private el: ElementRef,
              readonly pwaInstall: PwaInstallService,
              private appComponent: AppComponent,
              readonly theme: ThemeService,
              readonly prefs: ReaderPrefsService) {
                this.appComponent.showButton.set(true);
                this.appComponent.backTarget.set(['/home']);
               }

  fontOptions: { id: ReaderFont; label: string }[] = [
    { id: 'serif', label: $localize`:@@settings.font.serif:נוטו סריף` },
    { id: 'heading', label: $localize`:@@settings.font.heading:פרנק רוהל` },
    { id: 'body', label: $localize`:@@settings.font.body:רוביק` },
  ];

  emailAddress = CONTACT_EMAIL;
  emailUsButton: string = $localize`:@@settings.emailUs:אימייל`;
  emailUsIcon:string = 'email-icon';
  whatsappUsButton: string = $localize`:@@settings.whatsappUs:וואטסאפ`;

  readonly downloadAppButton = computed(() => {
    if (this.pwaInstall.isStandalone()) return $localize`:@@settings.appInstalled:האפליקציה מותקנת`;
    if (this.pwaInstall.isIos()) return $localize`:@@settings.addToHomeScreen:הוספה למסך הבית`;
    return $localize`:@@settings.downloadApp:הורדת אפליקציה`;
  });
  downloadAppIcon:string = 'download-icon';

  ngOnInit(): void {  }

  emailUs(){
    const mailtoLink = this.renderer.createElement('a');
    this.renderer.setProperty(mailtoLink, 'href', 'mailto:' + this.emailAddress);
    this.renderer.appendChild(this.el.nativeElement, mailtoLink);
    mailtoLink.click();
  }

  whatsappUs(){
    window.open(WHATSAPP_CONTACT_URL, '_blank', 'noopener');
  }

  downloadApp(){
    if (this.pwaInstall.isStandalone()) return;
    this.pwaInstall.installPWA();
  }
}
