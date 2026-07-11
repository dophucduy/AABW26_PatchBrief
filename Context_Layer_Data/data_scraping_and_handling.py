from __future__ import annotations

import json
import time
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

import requests


# ============================================================
# CẤU HÌNH
# ============================================================
# Thay bằng App ID của game cần lấy review.
APP_ID = 1623730
# Chỉ lấy review trong 30 ngày gần nhất.
DAYS_BACK = 30

# "all", "english", "vietnamese", ...
LANGUAGE = "all"

# "all", "positive", hoặc "negative"
REVIEW_TYPE = "all"

# "all", "steam", hoặc "non_steam_purchase"
PURCHASE_TYPE = "all"

# Steam hỗ trợ tối đa 100 review mỗi request.
REVIEWS_PER_PAGE = 100

# Thời gian nghỉ giữa các request.
REQUEST_DELAY_SECONDS = 1.0

# Giới hạn an toàn.
# Đặt None nếu muốn lấy toàn bộ review trong 30 ngày.
MAX_REVIEWS: int | None = None

OUTPUT_JSON = Path("data/steam_reviews_last_30_days_palworld.json")
OUTPUT_JSONL = Path("data/steam_reviews_last_30_days_palworld.jsonl")


# ============================================================
# HÀM XỬ LÝ
# ============================================================

def normalize_review(raw_review: dict[str, Any]) -> dict[str, Any]:
    """
    Chuyển một review gốc từ Steam thành schema gọn của hệ thống.
    """

    author = raw_review.get("author") or {}

    return {
        "review_id": str(
            raw_review.get("recommendationid") or ""
        ),
        "source": "steam",
        "language": raw_review.get("language"),
        "review": raw_review.get("review") or "",
        "voted_up": raw_review.get("voted_up"),
        "votes_up": raw_review.get("votes_up"),
        "refunded": raw_review.get("refunded"),
        "written_during_early_access": raw_review.get(
            "written_during_early_access"
        ),
        "timestamp_created": raw_review.get(
            "timestamp_created"
        ),
        "timestamp_updated": raw_review.get(
            "timestamp_updated"
        ),
        "playtime_at_review_minutes": author.get(
            "playtime_at_review"
        ),
    }


def fetch_recent_steam_reviews(
    app_id: int,
    days_back: int = 30,
    language: str = "all",
    review_type: str = "all",
    purchase_type: str = "all",
    max_reviews: int | None = None,
) -> list[dict[str, Any]]:
    """
    Lấy các review được tạo trong N ngày gần nhất.

    Steam được gọi bằng filter=recent, tức review mới hơn được
    trả trước. Khi gặp review cũ hơn cutoff, pipeline dừng.
    """

    endpoint = (
        f"https://store.steampowered.com/appreviews/{app_id}"
    )

    now_utc = datetime.now(timezone.utc)
    cutoff_datetime = now_utc - timedelta(days=days_back)
    cutoff_timestamp = int(cutoff_datetime.timestamp())

    cursor = "*"
    page_number = 0
    reached_cutoff = False

    collected: list[dict[str, Any]] = []
    seen_review_ids: set[str] = set()

    session = requests.Session()
    session.headers.update(
        {
            "User-Agent": (
                "GameDesignResearch-SteamReviewCollector/1.0"
            )
        }
    )

    print(
        f"Lấy review từ: "
        f"{cutoff_datetime.isoformat()} đến {now_utc.isoformat()}"
    )

    while not reached_cutoff:
        page_number += 1

        params = {
            "json": 1,
            "filter": "recent",
            "language": language,
            "review_type": review_type,
            "purchase_type": purchase_type,
            "num_per_page": REVIEWS_PER_PAGE,
            "cursor": cursor,
            # Giữ nguyên review bị Steam phân loại là off-topic.
            "filter_offtopic_activity": 0,
        }

        try:
            response = session.get(
                endpoint,
                params=params,
                timeout=30,
            )
            response.raise_for_status()
        except requests.RequestException as error:
            raise RuntimeError(
                f"Lỗi khi gọi Steam ở trang {page_number}: {error}"
            ) from error

        try:
            payload = response.json()
        except requests.JSONDecodeError as error:
            raise RuntimeError(
                "Steam không trả về JSON hợp lệ."
            ) from error

        if payload.get("success") != 1:
            raise RuntimeError(
                f"Steam trả về response lỗi: {payload}"
            )

        page_reviews = payload.get("reviews") or []

        if not page_reviews:
            print("Steam không còn trả thêm review.")
            break

        accepted_on_page = 0
        skipped_duplicate = 0
        skipped_invalid = 0

        for raw_review in page_reviews:
            timestamp_created = raw_review.get(
                "timestamp_created"
            )

            try:
                created_timestamp = int(timestamp_created)
            except (TypeError, ValueError):
                skipped_invalid += 1
                continue

            # filter=recent trả review theo thời gian tạo,
            # từ mới đến cũ.
            if created_timestamp < cutoff_timestamp:
                reached_cutoff = True
                break

            review_id = str(
                raw_review.get("recommendationid") or ""
            )

            if not review_id:
                skipped_invalid += 1
                continue

            if review_id in seen_review_ids:
                skipped_duplicate += 1
                continue

            normalized = normalize_review(raw_review)

            seen_review_ids.add(review_id)
            collected.append(normalized)
            accepted_on_page += 1

            if (
                max_reviews is not None
                and len(collected) >= max_reviews
            ):
                break

        print(
            f"Trang {page_number}: "
            f"Steam trả {len(page_reviews)}, "
            f"giữ {accepted_on_page}, "
            f"trùng {skipped_duplicate}, "
            f"không hợp lệ {skipped_invalid}, "
            f"tổng {len(collected)}"
        )

        if (
            max_reviews is not None
            and len(collected) >= max_reviews
        ):
            print(
                f"Đã đạt giới hạn {max_reviews} review."
            )
            break

        if reached_cutoff:
            print(
                f"Đã gặp review cũ hơn {days_back} ngày."
            )
            break

        next_cursor = payload.get("cursor")

        if not next_cursor:
            print("Response không có cursor tiếp theo.")
            break

        if next_cursor == cursor:
            print(
                "Cursor không thay đổi. "
                "Dừng để tránh vòng lặp vô hạn."
            )
            break

        cursor = next_cursor
        time.sleep(REQUEST_DELAY_SECONDS)

    return collected


def save_json(
    reviews: list[dict[str, Any]],
    output_path: Path,
) -> None:
    """
    Lưu toàn bộ review trong một JSON array.
    """

    output_path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    with output_path.open(
        "w",
        encoding="utf-8",
    ) as file:
        json.dump(
            reviews,
            file,
            ensure_ascii=False,
            indent=2,
        )


def save_jsonl(
    reviews: list[dict[str, Any]],
    output_path: Path,
) -> None:
    """
    Lưu mỗi review trên một dòng JSON.
    Phù hợp cho data pipeline và xử lý bằng AI.
    """

    output_path.parent.mkdir(
        parents=True,
        exist_ok=True,
    )

    with output_path.open(
        "w",
        encoding="utf-8",
    ) as file:
        for review in reviews:
            file.write(
                json.dumps(
                    review,
                    ensure_ascii=False,
                )
                + "\n"
            )


def print_summary(
    reviews: list[dict[str, Any]],
) -> None:
    positive_count = sum(
        review.get("voted_up") is True
        for review in reviews
    )

    negative_count = sum(
        review.get("voted_up") is False
        for review in reviews
    )

    refunded_count = sum(
        review.get("refunded") is True
        for review in reviews
    )

    early_access_count = sum(
        review.get("written_during_early_access") is True
        for review in reviews
    )

    print("\n===== TỔNG KẾT =====")
    print(f"Tổng review: {len(reviews)}")
    print(f"Recommended: {positive_count}")
    print(f"Not Recommended: {negative_count}")
    print(f"Refunded: {refunded_count}")
    print(
        "Written during Early Access: "
        f"{early_access_count}"
    )


def main() -> None:
    reviews = fetch_recent_steam_reviews(
        app_id=APP_ID,
        days_back=DAYS_BACK,
        language=LANGUAGE,
        review_type=REVIEW_TYPE,
        purchase_type=PURCHASE_TYPE,
        max_reviews=MAX_REVIEWS,
    )

    save_json(
        reviews=reviews,
        output_path=OUTPUT_JSON,
    )

    save_jsonl(
        reviews=reviews,
        output_path=OUTPUT_JSONL,
    )

    print_summary(reviews)

    print(f"\nJSON:  {OUTPUT_JSON.resolve()}")
    print(f"JSONL: {OUTPUT_JSONL.resolve()}")


if __name__ == "__main__":
    main()