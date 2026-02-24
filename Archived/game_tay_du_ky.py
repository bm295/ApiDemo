import json
import os
import tkinter as tk
from dataclasses import dataclass
from pathlib import Path
from tkinter import filedialog, messagebox
from typing import Dict, List

QUESTIONS_FILE = Path(__file__).with_name("questions.json")


@dataclass
class Question:
    prompt: str
    options: Dict[str, str]
    correct: str


@dataclass
class Level:
    level_number: int
    title: str
    questions: List[Question]


@dataclass
class GameState:
    current_level: int
    current_question_index: int

    def clamp(self, max_level: int) -> None:
        if self.current_level < 1:
            self.current_level = 1
        if self.current_level > max_level:
            self.current_level = max_level
        if self.current_question_index < 0:
            self.current_question_index = 0


def load_levels(path: Path) -> List[Level]:
    with path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    levels: List[Level] = []
    for item in payload.get("levels", []):
        questions = [
            Question(
                prompt=question["question"],
                options=question["options"],
                correct=question["correct"],
            )
            for question in item.get("questions", [])
        ]
        levels.append(
            Level(
                level_number=int(item.get("level", len(levels) + 1)),
                title=item.get("title", f"Nạn kiếp {len(levels) + 1}"),
                questions=questions,
            )
        )
    return levels


def normalize_answer(text: str) -> str:
    return " ".join(text.strip().casefold().split())


def save_game(state: GameState, path: str, max_level: int) -> None:
    state.clamp(max_level)
    payload = {
        "current_level": state.current_level,
        "current_question_index": state.current_question_index,
    }
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)


def load_game(path: str, max_level: int) -> GameState:
    with open(path, "r", encoding="utf-8") as handle:
        payload = json.load(handle)
    state = GameState(
        current_level=int(payload.get("current_level", 1)),
        current_question_index=int(payload.get("current_question_index", 0)),
    )
    state.clamp(max_level)
    return state


class TayDuKyGame(tk.Tk):
    _NO_SELECTION = "__none__"

    def __init__(self) -> None:
        super().__init__()
        self.title("TÂY DU KÝ - 81 NẠN KIẾP")
        self.geometry("720x480")
        self.resizable(False, False)

        if not QUESTIONS_FILE.exists():
            messagebox.showerror(
                "Lỗi dữ liệu",
                f"Không tìm thấy file câu hỏi: {QUESTIONS_FILE}",
            )
            raise SystemExit(1)

        self.levels = load_levels(QUESTIONS_FILE)
        if not self.levels:
            messagebox.showerror("Lỗi dữ liệu", "Danh sách câu hỏi đang trống.")
            raise SystemExit(1)

        self.level_map: Dict[int, Level] = {level.level_number: level for level in self.levels}
        self.max_level = max(self.level_map)
        self.state = GameState(current_level=1, current_question_index=0)

        self.menu_frame = tk.Frame(self)
        self.game_frame = tk.Frame(self)

        self._build_menu()
        self._build_game_screen()
        self.show_menu()

    def _build_menu(self) -> None:
        label = tk.Label(
            self.menu_frame,
            text="TÂY DU KÝ - 81 NẠN KIẾP",
            font=("Helvetica", 18, "bold"),
        )
        label.pack(pady=24)

        btn_new = tk.Button(self.menu_frame, text="New Game", width=20, command=self.new_game)
        btn_load = tk.Button(self.menu_frame, text="Load Game", width=20, command=self.load_game_flow)
        btn_exit = tk.Button(self.menu_frame, text="Thoát", width=20, command=self.destroy)

        btn_new.pack(pady=8)
        btn_load.pack(pady=8)
        btn_exit.pack(pady=8)

    def _build_game_screen(self) -> None:
        self.level_label = tk.Label(self.game_frame, text="", font=("Helvetica", 14, "bold"))
        self.title_label = tk.Label(self.game_frame, text="", font=("Helvetica", 12))
        self.question_label = tk.Label(self.game_frame, text="", wraplength=640, font=("Helvetica", 12))
        self.choice_var = tk.StringVar(value=self._NO_SELECTION)
        self.choices_frame = tk.Frame(self.game_frame)
        self.feedback_label = tk.Label(self.game_frame, text="", fg="red")

        button_frame = tk.Frame(self.game_frame)
        self.submit_button = tk.Button(button_frame, text="Trả lời", command=self.submit_answer)
        self.save_button = tk.Button(button_frame, text="Save Game", command=self.save_game_flow)
        self.back_button = tk.Button(button_frame, text="Menu chính", command=self.show_menu)

        self.level_label.pack(pady=(20, 6))
        self.title_label.pack(pady=(0, 12))
        self.question_label.pack(pady=(0, 12))
        self.choices_frame.pack(pady=(0, 8))
        self.feedback_label.pack(pady=(0, 12))

        button_frame.pack(pady=10)
        self.submit_button.pack(side=tk.LEFT, padx=6)
        self.save_button.pack(side=tk.LEFT, padx=6)
        self.back_button.pack(side=tk.LEFT, padx=6)

    def show_menu(self) -> None:
        self.game_frame.pack_forget()
        self.menu_frame.pack(fill=tk.BOTH, expand=True)

    def show_game(self) -> None:
        self.menu_frame.pack_forget()
        self.game_frame.pack(fill=tk.BOTH, expand=True)
        self.refresh_question()

    def new_game(self) -> None:
        self.state = GameState(current_level=1, current_question_index=0)
        self.show_game()

    def load_game_flow(self) -> None:
        path = filedialog.askopenfilename(
            title="Chọn file save",
            filetypes=[("JSON files", "*.json"), ("All files", "*.*")],
        )
        if not path:
            return
        try:
            self.state = load_game(path, self.max_level)
        except (json.JSONDecodeError, OSError, ValueError) as exc:
            messagebox.showerror("Lỗi", f"Không thể đọc file save: {exc}")
            return
        self.show_game()

    def save_game_flow(self) -> None:
        path = filedialog.asksaveasfilename(
            title="Lưu game",
            defaultextension=".json",
            filetypes=[("JSON files", "*.json"), ("All files", "*.*")],
            initialfile="tay_du_ky_save.json",
        )
        if not path:
            return
        os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
        try:
            save_game(self.state, path, self.max_level)
        except OSError as exc:
            messagebox.showerror("Lỗi", f"Không thể lưu game: {exc}")
            return
        messagebox.showinfo("Lưu game", f"Đã lưu tiến trình tại: {path}")

    def current_level(self) -> Level:
        return self.level_map[self.state.current_level]

    def refresh_question(self) -> None:
        level = self.current_level()
        total_questions = len(level.questions)
        if total_questions == 0:
            self.question_label.config(text="Màn này chưa có câu hỏi. Vui lòng cập nhật file câu hỏi.")
            self._clear_choices()
            self.submit_button.config(state=tk.DISABLED)
            return

        if self.state.current_question_index >= total_questions:
            self.state.current_question_index = total_questions - 1

        question = level.questions[self.state.current_question_index]
        self.level_label.config(
            text=f"Màn {level.level_number}/{self.max_level} - Câu {self.state.current_question_index + 1}/{total_questions}"
        )
        self.title_label.config(text=level.title)
        self.question_label.config(text=question.prompt)
        self.feedback_label.config(text="", fg="red")
        self.choice_var.set(self._NO_SELECTION)
        self._render_choices(question)
        self.submit_button.config(state=tk.NORMAL)

    def submit_answer(self) -> None:
        level = self.current_level()
        question = level.questions[self.state.current_question_index]
        selected = self.choice_var.get()
        normalized_selected = normalize_answer(selected)
        normalized_correct = normalize_answer(question.correct)

        if selected == self._NO_SELECTION or selected not in question.options:
            self.feedback_label.config(text="Bạn chưa chọn câu trả lời.")
            return
        if normalized_selected != normalized_correct:
            self.feedback_label.config(text="Sai rồi, hãy thử lại.")
            return

        self.feedback_label.config(text="Chính xác!", fg="green")
        if self.state.current_question_index + 1 < len(level.questions):
            self.state.current_question_index += 1
            self.after(300, self.refresh_question)
            return

        if self.state.current_level >= self.max_level:
            messagebox.showinfo("Hoàn thành", "Bạn đã vượt qua tất cả các màn!")
            self.show_menu()
            return

        self.state.current_level += 1
        self.state.current_question_index = 0
        messagebox.showinfo("Qua màn", "Bạn đã vượt qua màn hiện tại!")
        self.refresh_question()

    def _clear_choices(self) -> None:
        for child in self.choices_frame.winfo_children():
            child.destroy()
        self.choice_var.set(self._NO_SELECTION)

    def _render_choices(self, question: Question) -> None:
        self._clear_choices()
        self.choice_var.set(self._NO_SELECTION)
        for key, label in question.options.items():
            button = tk.Radiobutton(
                self.choices_frame,
                text=f"{key}. {label}",
                value=key,
                variable=self.choice_var,
                anchor="w",
                justify="left",
                wraplength=620,
            )
            button.pack(fill="x", pady=2)


def main() -> None:
    app = TayDuKyGame()
    app.mainloop()


if __name__ == "__main__":
    main()
