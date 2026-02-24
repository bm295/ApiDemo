import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Goal } from './models/goal.model';
import { GoalAiService } from './services/goal-ai.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  newGoalTitle = 'Hoàn thành chứng chỉ Data Analytics trong 3 tháng';

  goal: Goal = {
    title: this.newGoalTitle,
    description: 'Mục tiêu phát triển kỹ năng nghề nghiệp với roadmap cụ thể.',
    deadline: '2026-06-01',
    tasks: []
  };

  constructor(private readonly aiService: GoalAiService) {
    this.generateTasks();
  }

  generateTasks(): void {
    this.goal.tasks = this.aiService.splitGoalIntoTasks(this.goal.title);
  }

  toggleTask(index: number): void {
    this.goal.tasks[index].done = !this.goal.tasks[index].done;
  }

  get progress(): number {
    return this.aiService.getProgress(this.goal);
  }

  get analysis(): string {
    return this.aiService.getProcrastinationAnalysis(this.goal);
  }

  get adjustment(): string {
    return this.aiService.getPlanAdjustment(this.goal);
  }
}
