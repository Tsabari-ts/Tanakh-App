import { Component, OnInit, ElementRef, Renderer2, ChangeDetectionStrategy } from '@angular/core';
import { PwaInstallService } from '../../services/pwa-install.service';
import { DialogService } from '../../services/dialog.service';
import { AppComponent } from '../../app.component';
import { NgClass } from '@angular/common';

@Component({
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.css',
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [NgClass]
})

export class SettingsComponent implements OnInit {
  constructor(private renderer: Renderer2,
              private el: ElementRef,
              private pwaInstall: PwaInstallService,
              private dialogService: DialogService,
              private appComponent: AppComponent) {
                this.appComponent.showButton = true;
               }

  emailAddress = 'Tanakhdev@gmail.com';
  isPwaInstalled = localStorage.getItem('pwaInstalled') === 'true';
  userHasSubscribed = localStorage.getItem('userHasSubscribed') === 'true'; 
  
  subscribeButton:string = this.userHasSubscribed ? 'נרשמת לתזכורת' : 'הירשם לתזכורת יומית';
  subscribeIcon:string = 'calendar-icon';
  contactUsButton: string = 'צור קשר';
  contactUsIcon:string = 'email-icon';
  downloadAppButton: string = this.isPwaInstalled ? 'האפליקציה מותקנת': 'הורדת אפליקציה';
  downloadAppIcon:string = 'download-icon';



  ngOnInit(): void {  }

  openSubscribe(){
  const dialogRef = this.dialogService.openSubscribeDialog();

  dialogRef.componentInstance.subscriptionStatusChange.subscribe(
    (changes:
       { newButtonName: string }
       ) => {
    this.subscribeButton = changes.newButtonName;
  });
  }

  contactUs(){
    const mailtoLink = this.renderer.createElement('a');
    this.renderer.setProperty(mailtoLink, 'href', 'mailto:' + this.emailAddress);
    this.renderer.appendChild(this.el.nativeElement, mailtoLink);
    mailtoLink.click();
  }

  downloadApp(){
    this.pwaInstall.installPWA();
  }
}