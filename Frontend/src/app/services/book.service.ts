import { Injectable } from '@angular/core';
import { BookData } from '../models/BookData';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  chapter: number[] = [];
  constructor() { }

  setBookData(data: BookData): void {
    if(data != null){
      this.chapter = data.chapters;
    }
  }

  getBookChapter(): number[] {
    return this.chapter;
  }
}
