// Sohbet sayfası (Views/Chat/Session.cshtml)
//
// - Mesajı sayfayı yenilemeden (fetch ile) gönderir, cevap gelene kadar "yazıyor…" gösterir.
// - Sunucu cevabı _ChatTurn partial view'ı olarak HTML döner; doğrudan listeye eklenir.
// - Enter gönderir, Shift+Enter yeni satır açar.
// - Sohbet toplamlarını (token, süre, maliyet) ve giriş token artışını hesaplar.
// JavaScript kapalıysa form normal POST ile çalışmaya devam eder.
(function () {
    "use strict";

    const form = document.getElementById("chat-form");
    if (!form) {
        return;
    }

    const messages = document.getElementById("chat-messages");
    const prompt = document.getElementById("chat-prompt");
    const sendButton = document.getElementById("chat-send");
    const spinner = sendButton.querySelector(".spinner-border");
    const sendIcon = sendButton.querySelector(".bi-send");
    const typing = document.getElementById("chat-typing");
    const empty = document.getElementById("chat-empty");
    const errorBox = document.getElementById("chat-error");
    const title = document.getElementById("chat-title");
    const summary = document.getElementById("chat-summary");

    const numberFormat = new Intl.NumberFormat("tr-TR");
    let busy = false;

    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function autoResize() {
        prompt.style.height = "auto";
        prompt.style.height = Math.min(prompt.scrollHeight, 200) + "px";
    }

    function setBusy(value) {
        busy = value;
        prompt.disabled = value;
        sendButton.disabled = value;
        spinner.classList.toggle("d-none", !value);
        sendIcon.classList.toggle("d-none", value);
        typing.classList.toggle("d-none", !value);
        if (value) {
            // "yazıyor…" balonu her zaman listenin en altında dursun.
            messages.appendChild(typing);
            scrollToBottom();
        }
    }

    function showError(message) {
        errorBox.textContent = message;
        errorBox.classList.toggle("d-none", !message);
    }

    // Her asistan cevabının altındaki giriş token sayısına bir önceki cevaba göre
    // artışı yazar (+42 gibi). Geçmiş her istekte yeniden gönderildiği için bu sayı büyür.
    function updateMetrics() {
        const rows = messages.querySelectorAll(".chat-metrics[data-input-tokens]");
        let input = 0, output = 0, latency = 0, cost = 0, previousInput = null;

        rows.forEach(function (row) {
            const inTokens = parseInt(row.dataset.inputTokens, 10) || 0;
            input += inTokens;
            output += parseInt(row.dataset.outputTokens, 10) || 0;
            latency += parseInt(row.dataset.latencyMs, 10) || 0;
            cost += parseFloat(row.dataset.cost) || 0;

            const delta = row.querySelector(".chat-input-delta");
            if (delta) {
                delta.textContent = previousInput !== null && inTokens > previousInput
                    ? "(+" + numberFormat.format(inTokens - previousInput) + ")"
                    : "";
            }
            previousInput = inTokens;
        });

        const set = function (key, value) {
            summary.querySelector('[data-total="' + key + '"]').textContent = value;
        };
        set("turns", numberFormat.format(rows.length));
        set("input", numberFormat.format(input));
        set("output", numberFormat.format(output));
        set("latency", numberFormat.format(rows.length ? Math.round(latency / rows.length) : 0));
        set("cost", "$" + cost.toFixed(6).replace(/\.?0+$/, "") );
    }

    async function send() {
        const text = prompt.value.trim();
        if (busy || !text) {
            return;
        }

        showError("");
        const body = new FormData(form); // anti-forgery token da formun içinde
        setBusy(true);

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: body,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) {
                let data = null;
                try { data = await response.json(); } catch (e) { /* JSON değil */ }

                if (data && data.redirectUrl) {
                    // API anahtarı yok: anahtar sayfasına gidip buraya geri dönülür.
                    window.location.href = data.redirectUrl;
                    return;
                }
                throw new Error((data && data.error) || "Mesaj gönderilemedi (" + response.status + ").");
            }

            const html = await response.text();
            messages.insertAdjacentHTML("beforeend", html);
            empty.classList.add("d-none");

            // İlk mesajdan sonra sunucu başlığı mesajdan üretir; sayfada da güncellenir.
            if (title.textContent.trim() === "Yeni sohbet") {
                const singleLine = text.split(/\s+/).join(" ");
                title.textContent = singleLine.length <= 60 ? singleLine : singleLine.slice(0, 57) + "...";
                document.title = title.textContent + " - PROMPT Lab";
            }

            prompt.value = "";
            autoResize();
            updateMetrics();
        } catch (err) {
            // Mesaj kaybolmasın diye kutuda bırakılır, kullanıcı tekrar deneyebilir.
            showError(err.message || "Beklenmeyen bir hata oluştu.");
        } finally {
            setBusy(false);
            prompt.focus();
            scrollToBottom();
        }
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();
        send();
    });

    prompt.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            send();
        }
    });

    prompt.addEventListener("input", autoResize);

    updateMetrics();
    scrollToBottom();
    if (!prompt.disabled) {
        prompt.focus();
    }
})();
