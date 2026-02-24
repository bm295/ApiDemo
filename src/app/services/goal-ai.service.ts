import { Injectable } from '@angular/core';
import { Goal, Task } from '../models/goal.model';

@Injectable({ providedIn: 'root' })
export class GoalAiService {
  splitGoalIntoTasks(goalTitle: string): Task[] {
    return [
      { title: `Xác định outcome chính cho: ${goalTitle}`, done: false },
      { title: 'Thiết lập thói quen 30 phút/ngày', done: false },
      { title: 'Review tiến độ mỗi tuần', done: false },
      { title: 'Tối ưu kế hoạch dựa trên dữ liệu trì hoãn', done: false }
    ];
  }

  getProgress(goal: Goal): number {
    if (!goal.tasks.length) return 0;
    const completed = goal.tasks.filter((task) => task.done).length;
    return Math.round((completed / goal.tasks.length) * 100);
  }

  getProcrastinationAnalysis(goal: Goal): string {
    const pending = goal.tasks.filter((task) => !task.done).length;

    if (pending === 0) {
      return 'Bạn đang duy trì đà rất tốt. Hãy tăng độ khó mục tiêu tiếp theo!';
    }

    if (pending >= Math.ceil(goal.tasks.length * 0.7)) {
      return 'AI nhận thấy bạn trì hoãn do mục tiêu còn quá lớn. Cần chia nhỏ thêm và đặt deadline ngắn hơn.';
    }

    return 'Bạn có dấu hiệu trì hoãn vào giữa chu kỳ. Gợi ý: nhắc nhở thông minh vào khung giờ bạn thường bỏ lỡ task.';
  }

  getPlanAdjustment(goal: Goal): string {
    const progress = this.getProgress(goal);

    if (progress < 30) {
      return 'Đề xuất điều chỉnh: giảm scope 20%, ưu tiên 1 task quan trọng/ngày trong 7 ngày tới.';
    }

    if (progress < 70) {
      return 'Đề xuất điều chỉnh: giữ mục tiêu, thêm checkpoint thứ 4 và chủ động nhắc vào buổi sáng.';
    }

    return 'Bạn đang đúng tiến độ. Hãy thêm nhiệm vụ nâng cao để vượt mục tiêu.';
  }
}
