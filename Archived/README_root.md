# Tây Du Ký - 81 Nạn Kiếp

Game mô phỏng hành trình thỉnh kinh với **81 màn chơi**, mỗi màn là một nạn kiếp. Người chơi phải **thắng từng màn** để mở khóa màn tiếp theo.

## Tính năng
- **New Game**: bắt đầu hành trình mới từ màn 1.
- **Load Game**: tải tiến trình đã lưu.
- **Save Game**: lưu tiến trình tại vị trí tùy chọn.
- **81 màn chơi** theo thứ tự, phải thắng mới qua màn.

## Chạy game
Yêu cầu: Python 3.10+.

```bash
python game_tay_du_ky.py
```

## Lưu/Load
- Khi chọn **Save Game**, bạn có thể nhập đường dẫn bất kỳ (ví dụ: `saves/hanh_gia.json`).
- Khi chọn **Load Game**, nhập đường dẫn đến file save đã lưu.

## Cấu trúc
- `game_tay_du_ky.py`: mã nguồn game chính.
- `Archived/`: các file cũ đã được lưu trữ.
