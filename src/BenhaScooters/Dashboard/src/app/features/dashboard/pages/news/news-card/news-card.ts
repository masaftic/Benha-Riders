import { Component, input, output } from '@angular/core';
import { NewsItem } from '../../../../../core/models/news.model';
import { ButtonModule } from "primeng/button";
import { Tooltip } from 'primeng/tooltip';

@Component({
  selector: 'app-news-card',
  imports: [ButtonModule, Tooltip],
  templateUrl: './news-card.html',
  styleUrl: './news-card.scss',
})
export class NewsCard {
  newsItem = input.required<NewsItem>();

  edit = output<void>();
  delete = output<void>();
}
