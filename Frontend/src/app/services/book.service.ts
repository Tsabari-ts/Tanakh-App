import { Injectable, signal } from '@angular/core';
import { BookData } from '../models/BookData';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  private readonly _chapter = signal<number[]>([]);
  readonly chapter = this._chapter.asReadonly();

  setBookData(data: BookData): void {
    if (data != null) {
      this._chapter.set(data.chapters);
    }
  }

  getBookChapter(): number[] {
    return this._chapter();
  }
}
