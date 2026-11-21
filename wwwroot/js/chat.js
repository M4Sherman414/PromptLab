// Sohbet sayfası (Views/Chat/Session.cshtml)
//
// - Kullanıcının mesajı hemen ekranda görünür, ardından cevap gelene kadar "yazıyor…" gösterilir.
// - Mesaj fetch ile gönderilir; sunucu model cevabını _ChatMessage partial view'ı olarak HTML döner.
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

    // Sunucudaki _ChatMessage partial'ının kullanıcı balonuyla aynı yapı.
    // textContent kullanıldığı için mesajdaki HTML çalıştırılmaz.
    function createUserRow(text) {
        const row = document.createElement("div");
        row.className = "chat-row chat-row-user";
        const bubble = document.createElement("div");
        bubble.className = "chat-bubble";
        const author = document.createElement("div");
        author.className = "chat-author";
        author.textContent = "Siz";
        const content = document.createElement("div");
        content.className = "chat-content";
        content.textContent = text;
        bubble.append(author, content);
        row.appendChild(bubble);
        return row;
    }

    async function send() {
        const text = prompt.value.trim();
        if (busy || !text) {
            return;
        }

        showError("");
        const body = new FormData(form); // anti-forgery token da formun içinde

        // 1) Kullanıcının mesajı hemen ekranda görünür, kutu temizlenir.
        const userRow = createUserRow(text);
        messages.insertBefore(userRow, typing);
        empty.classList.add("d-none");
        prompt.value = "";
        autoResize();

        // 2) Ardından model cevabı beklenirken "yazıyor…" gösterilir.
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

            // 3) Sunucu yalnızca asistan cevabını (_ChatMessage partial) döner; "yazıyor…" balonunun yerine geçer.
            const html = await response.text();
            typing.insertAdjacentHTML("beforebegin", html);

            // İlk mesajdan sonra sunucu başlığı mesajdan üretir; sayfada da güncellenir.
            if (title.textContent.trim() === "Yeni sohbet") {
                const singleLine = text.split(/\s+/).join(" ");
                title.textContent = singleLine.length <= 60 ? singleLine : singleLine.slice(0, 57) + "...";
                document.title = title.textContent + " - PROMPT Lab";
            }

            updateMetrics();
        } catch (err) {
            // İstek başarısızsa sunucu mesajı kaydetmemiştir: balon kaldırılır,
            // metin kutuya geri konur, kullanıcı tekrar deneyebilir.
            userRow.remove();
            if (!messages.querySelector(".chat-row:not(#chat-typing)")) {
                empty.classList.remove("d-none");
            }
            prompt.value = text;
            autoResize();
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
