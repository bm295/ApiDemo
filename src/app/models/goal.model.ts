export interface Task {
  title: string;
  done: boolean;
}

export interface Goal {
  title: string;
  description: string;
  deadline: string;
  tasks: Task[];
}
