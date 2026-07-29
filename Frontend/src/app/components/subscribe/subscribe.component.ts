import { Component, EventEmitter, Inject, Output } from '@angular/core';
import { ApiCallService } from '../../services/api-call.service';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-subscribe',
  templateUrl: './subscribe.component.html',
  styleUrl: './subscribe.component.css'
})

export class SubscribeComponent {
  @Output() subscriptionStatusChange: EventEmitter<{
     newButtonName: string }> = new EventEmitter();
  private markSubscribeKey = 'userHasSubscribed';
  serverResponse:string = '';
  response: any;
  userData = {
    userName: '',
    phoneNumber: '',
    selectedTime: '',
  };
  hours: string[] = [];
  userHasSubscribed = false;
  isButtonDisabled: boolean = false;
isRequestInProgress: boolean = false;
isRequestSuccessful: boolean = false;
progressValue = 0;
loadingInterval: any;
subscribeSuccessful = false;
unSubscribeSuccessful = false;
inputValue: string = '';

nameValue: string = '';
phoneValue: string = '';
timeValue: string = '';
timeOptions: string[] = this.generateTimeOptions();

  constructor(@Inject(MAT_DIALOG_DATA) public data: any,
    public dialogRef: MatDialogRef<SubscribeComponent>,
    private apiService: ApiCallService) {
      this.userHasSubscribed = localStorage.getItem('userHasSubscribed') === 'true';
      for (let i = 8; i <= 20; i++) {
        this.hours.push(`${i}:00`);
        this.hours.push(`${i}:30`);
      }
    }
    
    generateTimeOptions(): string[] {
      const options: string[] = [];
      for (let hour = 8; hour <= 20; hour++) {
          options.push(`${hour}:00`);
      }
      return options;
    }

closeDialog() {
this.dialogRef.close();
}


submitForm(form: any) {
  if (form.valid) {
    const formData = {
      userName: this.nameValue,
      phoneNumber: this.phoneValue,
      selectedTime: this.timeValue
    };
    console.log(formData);
    this.closeAndSubscribe(formData);
  } else {
    form.submitted = true;
    console.log("error");
  }
}

  isValidPhoneNumber(): boolean {
    return /^\d{3}\d{7}$/.test(this.phoneValue);
  }

closeAndSubscribe(userData:any){
   this.isButtonDisabled = true;
   this.isRequestInProgress = true;
  this.startLoading();
let subscribeEntity = {UserName: userData.userName, PhoneNumber: userData.phoneNumber, SelectedTime: userData.selectedTime};
this.apiService.RegisterNewUser(subscribeEntity).subscribe(response => {
  this.subscribeSuccessful = response;
  if(this.subscribeSuccessful){
    this.markSubscribe();
  } 
}, (error) => {
  console.log(error);
  this.setSubscribeServerResponse();
},
() => {  
  setTimeout(() => {
    this.stopLoading();
    this.setSubscribeServerResponse();      
  }, 3000);
});
}

setSubscribeServerResponse(){
  setTimeout(() => {
    if (this.subscribeSuccessful) {
      this.serverResponse = 'הרישום בוצע בהצלחה! החל ממחר תקבל תזכורת';
      this.subscriptionStatusChange.emit({
        newButtonName: 'בטל קבלת תזכורת' });
    } else {
      this.serverResponse = 'הרישום נכשל, אנא נסה שוב מאוחר יותר';        
    }
    this.isRequestInProgress = false;
    this.isRequestSuccessful = true;

    setTimeout(() => {
      this.dialogRef.close();
    }, 3000);  
  }, 3000);  
}

startLoading(): void {
  this.progressValue = 0;
  const duration = 3000;
  const interval = 10; 
  const steps = (duration / interval);
  const stepSize = 100 / steps;

  this.loadingInterval = setInterval(() => {
    if (this.progressValue < 100) {
      this.progressValue += stepSize;
    } else {
      this.stopLoading();
    }
  }, interval);

  setTimeout(() => {
    this.stopLoading();
  }, duration);
}

stopLoading(): void {
  clearInterval(this.loadingInterval);
}


submitUnSubscribeForm(form: any) {
  if (form.valid) {
    const formData = {
      phoneNumber: this.phoneValue,
    };
    console.log(formData);
    this.closeAndUnSubscribe(formData);
  } else {
    form.submitted = true;
    console.log("error");
  }
}

closeAndUnSubscribe(userData:any){
  this.isButtonDisabled = true;
   this.isRequestInProgress = true;
  this.startLoading();
let unSubscribe = {phoneNumber: userData.phoneNumber};
this.apiService.DeleteSubscribedUser(unSubscribe).subscribe(
  response => {
  this.unSubscribeSuccessful = response;
  if(this.unSubscribeSuccessful){
    this.deleteSubscribeStorage();
  } 
}, (error) => {
  console.log(error);
  this.setUnSubscribeServerResponse();
},
() => {  
  setTimeout(() => {
    this.stopLoading();
    this.setUnSubscribeServerResponse();
  }, 3000);
});
}

setUnSubscribeServerResponse(){
  setTimeout(() => {
    if (this.unSubscribeSuccessful) {
      this.serverResponse = 'הביטול בוצע בהצלחה';
      this.subscriptionStatusChange.emit({
        newButtonName: 'הירשם לקבלת תזכורת' });
    } else {
      this.serverResponse = 'הביטול נכשל, אנא נסה שוב מאוחר יותר';        
    }
    this.isRequestInProgress = false;
    this.isRequestSuccessful = true;

    setTimeout(() => {
      this.dialogRef.close();
    }, 3000);  
  }, 3000);  
}

markSubscribe(): void {
  localStorage.setItem(this.markSubscribeKey, 'true');
}

deleteSubscribeStorage(): void {
  localStorage.removeItem(this.markSubscribeKey);
}
}
