#!/usr/bin/env python3
"""OpenRouter terminal chat (streaming).

- Ek paket gerekmez, sadece Python 3.8+ yeter.
- API key: OPENROUTER_API_KEY ortam değişkeni (yoksa başta sorar).
- Konuşmalar scriptin yanındaki  chats/<model>/  klasörüne kaydedilir.

Komutlar:
  /model       model id değiştir
  /new         aynı modelde yeni sohbet başlat
  /reasoning   düşünme (reasoning) çıktısını aç/kapat
  /help        komutları göster
  /exit        çık
"""
import getpass
import json
import os
import re
import sys
import urllib.error
import urllib.request
from datetime import datetime
from pathlib import Path

API_URL = "https://openrouter.ai/api/v1/chat/completions"
BASE_DIR = Path(__file__).resolve().parent / "chats"
LAST_MODEL_FILE = BASE_DIR / ".last_model"
DEFAULT_MODEL = "nvidia/nemotron-3-ultra-550b-a55b:free"

GRAY = "\033[90m"
CYAN = "\033[96m"
GREEN = "\033[92m"
RED = "\033[91m"
RESET = "\033[0m"


# ---------- yardımcılar ----------

def setup_console():
    # Windows'ta Türkçe karakter ve ANSI renkleri için
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except Exception:
            pass
    if os.name == "nt":
        os.system("")  # ANSI escape desteğini açar


def safe_name(model: str) -> str:
    """Model id'sini Windows'ta geçerli klasör adına çevirir (: ve / yasak)."""
    return re.sub(r'[<>:"/\\|?*\s]+', "_", model).strip("_")


def model_dir(model: str) -> Path:
    d = BASE_DIR / safe_name(model)
    d.mkdir(parents=True, exist_ok=True)
    return d


def get_api_key() -> str:
    key = os.environ.get("OPENROUTER_API_KEY", "").strip()
    if not key:
        key = getpass.getpass("OPENROUTER_API_KEY bulunamadı. Key'i gir (gizli yazılır): ").strip()
    if not key:
        sys.exit("API key yok, çıkılıyor.")
    return key


def ask_model() -> str:
    default = DEFAULT_MODEL
    if LAST_MODEL_FILE.exists():
        default = LAST_MODEL_FILE.read_text(encoding="utf-8").strip() or DEFAULT_MODEL
    entered = input(f"Model id [{default}]: ").strip()
    # [google](https://...)/gemma... gibi markdown link yapıştırılırsa temizle
    entered = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", entered)
    model = entered or default
    BASE_DIR.mkdir(parents=True, exist_ok=True)
    LAST_MODEL_FILE.write_text(model, encoding="utf-8")
    return model


# ---------- kayıt ----------

def new_session(model: str) -> dict:
    return {
        "id": datetime.now().strftime("%Y-%m-%d_%H-%M-%S"),
        "model": model,
        "messages": [],  # {role, content, reasoning?, usage?}
    }


def save_session(sess: dict):
    folder = model_dir(sess["model"])
    (folder / f"{sess['id']}.json").write_text(
        json.dumps(sess, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    # okunabilir kopya
    lines = [f"# {sess['model']} - {sess['id']}\n"]
    for m in sess["messages"]:
        who = "Sen" if m["role"] == "user" else "Model"
        lines.append(f"## {who}\n\n{m['content']}\n")
    (folder / f"{sess['id']}.md").write_text("\n".join(lines), encoding="utf-8")


def load_latest_session(model: str):
    folder = model_dir(model)
    files = sorted(folder.glob("*.json"))
    if not files:
        return None
    try:
        return json.loads(files[-1].read_text(encoding="utf-8"))
    except Exception:
        return None


# ---------- API ----------

def stream_chat(api_key: str, model: str, messages: list, show_reasoning: bool):
    """curl komutundakiyle aynı istek: POST + stream:true. (content, reasoning, usage) döner."""
    body = {"model": model, "messages": messages, "stream": True}
    req = urllib.request.Request(
        API_URL,
        data=json.dumps(body).encode("utf-8"),
        headers={
            "Content-Type": "application/json",
            "Authorization": f"Bearer {api_key}",
        },
        method="POST",
    )

    content_parts, reasoning_parts = [], []
    usage = None
    state = None  # None / "reasoning" / "content"

    with urllib.request.urlopen(req, timeout=300) as resp:
        for raw in resp:
            line = raw.decode("utf-8", errors="replace").strip()
            if not line or line.startswith(":"):  # ": OPENROUTER PROCESSING" vb.
                continue
            if not line.startswith("data:"):
                continue
            data = line[5:].strip()
            if data == "[DONE]":
                break
            try:
                chunk = json.loads(data)
            except json.JSONDecodeError:
                continue

            if "error" in chunk:
                err = chunk["error"]
                msg = err.get("message", err) if isinstance(err, dict) else err
                raise RuntimeError(f"Akış sırasında hata: {msg}")

            if chunk.get("usage"):
                usage = chunk["usage"]

            for choice in chunk.get("choices", []):
                delta = choice.get("delta") or {}
                r = delta.get("reasoning")
                c = delta.get("content")

                if r:
                    reasoning_parts.append(r)
                    if show_reasoning:
                        if state != "reasoning":
                            print(f"{GRAY}[düşünüyor] ", end="", flush=True)
                            state = "reasoning"
                        print(r, end="", flush=True)

                if c:
                    if state != "content":
                        if state == "reasoning":
                            print(f"{RESET}\n")
                        print(f"{GREEN}Model:{RESET} ", end="", flush=True)
                        state = "content"
                    content_parts.append(c)
                    print(c, end="", flush=True)

    print(RESET)
    return "".join(content_parts), "".join(reasoning_parts), usage


# ---------- ana döngü ----------

def start_chat(model: str) -> dict:
    prev = load_latest_session(model)
    if prev and prev.get("messages"):
        n = len(prev["messages"])
        ans = input(f"Bu modelde önceki sohbet var ({prev['id']}, {n} mesaj). Devam edilsin mi? [e/H]: ").strip().lower()
        if ans in ("e", "evet", "y", "yes"):
            print(f"{GRAY}Önceki sohbet yüklendi.{RESET}")
            return prev
    return new_session(model)


def main():
    setup_console()
    api_key = get_api_key()
    model = ask_model()
    sess = start_chat(model)
    show_reasoning = True

    print(f"\n{CYAN}Model: {model}{RESET}")
    print(f"{GRAY}Kayıt klasörü: {model_dir(model)}{RESET}")
    print(f"{GRAY}Komutlar için /help yaz.{RESET}\n")

    while True:
        try:
            prompt = input(f"{CYAN}Sen:{RESET} ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nÇıkılıyor.")
            break

        if not prompt:
            continue

        # --- komutlar ---
        if prompt in ("/exit", "/quit", "/q"):
            break
        if prompt == "/help":
            print(__doc__)
            continue
        if prompt == "/reasoning":
            show_reasoning = not show_reasoning
            print(f"{GRAY}Reasoning gösterimi: {'açık' if show_reasoning else 'kapalı'}{RESET}")
            continue
        if prompt == "/new":
            sess = new_session(model)
            print(f"{GRAY}Yeni sohbet başlatıldı.{RESET}")
            continue
        if prompt == "/model":
            model = ask_model()
            sess = start_chat(model)
            print(f"{CYAN}Model: {model}{RESET}")
            print(f"{GRAY}Kayıt klasörü: {model_dir(model)}{RESET}")
            continue

        # --- mesaj gönder ---
        sess["messages"].append({"role": "user", "content": prompt})
        api_messages = [{"role": m["role"], "content": m["content"]} for m in sess["messages"]]

        try:
            content, reasoning, usage = stream_chat(api_key, sess["model"], api_messages, show_reasoning)
        except urllib.error.HTTPError as e:
            sess["messages"].pop()  # başarısız mesajı geçmişten çıkar, tekrar deneyebilirsin
            detail = e.read().decode("utf-8", errors="replace")
            print(f"{RED}HTTP {e.code}:{RESET} {detail}")
            if e.code == 429:
                print(f"{GRAY}İpucu: model havuzu dolu. Biraz bekle ya da /model ile openrouter/free dene.{RESET}")
            elif e.code == 404:
                print(f"{GRAY}İpucu: model id geçersiz ya da listeden kalkmış. /model ile değiştir.{RESET}")
            elif e.code == 401:
                print(f"{GRAY}İpucu: API key hatalı ya da eksik.{RESET}")
            continue
        except KeyboardInterrupt:
            sess["messages"].pop()
            print(f"\n{GRAY}İptal edildi.{RESET}")
            continue
        except (urllib.error.URLError, RuntimeError, TimeoutError) as e:
            sess["messages"].pop()
            print(f"{RED}Hata:{RESET} {e}")
            continue

        if not content:
            sess["messages"].pop()
            print(f"{GRAY}(Model boş cevap döndü, mesaj geçmişe eklenmedi.){RESET}")
            continue

        entry = {"role": "assistant", "content": content}
        if reasoning:
            entry["reasoning"] = reasoning
        if usage:
            entry["usage"] = usage
        sess["messages"].append(entry)
        save_session(sess)

        if usage:
            print(
                f"{GRAY}[token: {usage.get('prompt_tokens', '?')} + "
                f"{usage.get('completion_tokens', '?')} | maliyet: {usage.get('cost', '?')}]{RESET}"
            )
        print()


if __name__ == "__main__":
    main()