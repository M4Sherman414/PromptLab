// Ana sayfadaki Sohbet / Karşılaştırma mod geçişi ve sayfa yenilemeden
// paralel model karşılaştırması.
(function () {
    "use strict";

    // 1) Result sayfasında yenileme sonrası hâlâ bekleyen slot varsa otomatik tamamla.
    const resultStage = document.getElementById("comparison-result-stage");
    if (resultStage) {
        const comparisonId = resultStage.dataset.comparisonId;
        const slotUrl = resultStage.dataset.slotUrl;
        const tokenInput = resultStage.querySelector('input[name="__RequestVerificationToken"]');
        const antiForgery = tokenInput ? tokenInput.value : "";

        ["A", "B"].forEach(function (slot) {
            const container = document.getElementById("slot-container-" + slot);
            const card = container && container.querySelector('[data-pending="true"]');
            if (comparisonId && slotUrl && card) {
                runSlot(comparisonId, slot, slotUrl, antiForgery, container);
            }
        });
    }

    // 2) Ana sayfa formu
    const form = document.getElementById("new-chat-form");
    const prompt = document.getElementById("new-chat-prompt");
    if (!form || !prompt) {
        return;
    }

    const root = document.getElementById("new-chat-root");
    const heading = document.getElementById("new-chat-heading");
    const modeInput = document.getElementById("mode-input");
    const modeButtons = document.querySelectorAll(".mode-switch-btn");
    const chatPicker = document.getElementById("chat-model-picker");
    const comparePickers = document.getElementById("compare-model-pickers");
    const compareParamsRow = document.getElementById("compare-params-row");
    const paramsLabel = document.getElementById("params-toggle-label");
    const modelASelect = document.getElementById("ModelAId");
    const modelBSelect = document.getElementById("ModelBId");
    const systemPromptInput = document.getElementById("SystemPrompt");
    const tempInput = document.getElementById("Temperature");
    const maxTokensInput = document.getElementById("MaxTokens");
    const submitBtn = document.getElementById("new-chat-submit");
    const spinner = submitBtn ? submitBtn.querySelector(".spinner-border") : null;
    const sendIcon = submitBtn ? submitBtn.querySelector(".bi-send") : null;
    const errorBox = document.getElementById("home-error");

    const battleStage = document.getElementById("battle-stage");
    const battlePromptText = document.getElementById("battle-prompt-text");
    const battleSystemNote = document.getElementById("battle-system-note");
    const slotAContainer = document.getElementById("slot-container-A");
    const slotBContainer = document.getElementById("slot-container-B");

    let busy = false;

    function getMode() {
        return modeInput ? modeInput.value : "chat";
    }

    function setMode(mode) {
        const isCompare = mode === "compare";
        if (modeInput) modeInput.value = isCompare ? "compare" : "chat";

        modeButtons.forEach(function (btn) {
            btn.classList.toggle("active", btn.dataset.mode === (isCompare ? "compare" : "chat"));
        });

        chatPicker?.classList.toggle("d-none", isCompare);
        comparePickers?.classList.toggle("d-none", !isCompare);
        compareParamsRow?.classList.toggle("d-none", !isCompare);

        if (paramsLabel) {
            paramsLabel.textContent = isCompare ? "Parametreler" : "System prompt";
        }
        if (heading) {
            heading.textContent = isCompare ? "İki modeli yarıştır" : "Nereden başlayalım?";
        }
        prompt.placeholder = isCompare
            ? "İki modele de sor… (Enter: gönder, Shift+Enter: yeni satır)"
            : "Bir şey sor… (Enter: gönder, Shift+Enter: yeni satır)";

        showError("");
    }

    function showError(message) {
        if (!errorBox) return;
        errorBox.textContent = message;
        errorBox.classList.toggle("d-none", !message);
    }

    function autoResize() {
        prompt.style.height = "auto";
        prompt.style.height = Math.min(prompt.scrollHeight, 240) + "px";
    }

    function setBusy(value) {
        busy = value;
        if (submitBtn) submitBtn.disabled = value;
        spinner?.classList.toggle("d-none", !value);
        sendIcon?.classList.toggle("d-none", value);
    }

    function escapeHtml(text) {
        const div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }

    function selectedOptionName(selectEl) {
        if (!selectEl || selectEl.selectedIndex < 0) return "Model";
        const raw = selectEl.options[selectEl.selectedIndex].textContent || "Model";
        return raw.split(" — ")[0].trim();
    }

    function renderLoadingCard(slot, modelName) {
        const badgeClass = slot === "A" ? "text-bg-primary" : "text-bg-dark";
        return (
            '<div class="comparison-card shadow-sm" data-slot="' + slot + '" data-pending="true">' +
                '<div class="comparison-card-header">' +
                    '<div class="d-flex align-items-center gap-2 text-truncate">' +
                        '<span class="badge ' + badgeClass + '">Model ' + slot + '</span>' +
                        '<span class="fw-semibold text-truncate">' + escapeHtml(modelName) + '</span>' +
                    '</div>' +
                    '<span class="spinner-border spinner-border-sm text-secondary" aria-hidden="true"></span>' +
                '</div>' +
                '<div class="comparison-card-body d-flex align-items-center">' +
                    '<div class="chat-typing">' +
                        '<span></span><span></span><span></span>' +
                        '<small class="text-muted ms-2">yanıt bekleniyor…</small>' +
                    '</div>' +
                '</div>' +
                '<div class="comparison-card-footer">' +
                    '<div class="chat-metrics">' +
                        '<span><i class="bi bi-hourglass-split"></i> ölçüm hazırlanıyor</span>' +
                    '</div>' +
                '</div>' +
            '</div>'
        );
    }

    function renderSlotErrorCard(slot, modelName, message) {
        const badgeClass = slot === "A" ? "text-bg-primary" : "text-bg-dark";
        return (
            '<div class="comparison-card shadow-sm" data-slot="' + slot + '">' +
                '<div class="comparison-card-header">' +
                    '<div class="d-flex align-items-center gap-2 text-truncate">' +
                        '<span class="badge ' + badgeClass + '">Model ' + slot + '</span>' +
                        '<span class="fw-semibold text-truncate">' + escapeHtml(modelName) + '</span>' +
                    '</div>' +
                    '<span class="badge text-bg-danger">Hata</span>' +
                '</div>' +
                '<div class="comparison-card-body">' +
                    '<div class="alert alert-danger mb-0">' + escapeHtml(message) + '</div>' +
                '</div>' +
            '</div>'
        );
    }

    async function runSlot(comparisonId, slot, slotUrl, antiForgery, container, fallbackModelName) {
        try {
            const body = new URLSearchParams();
            body.set("id", comparisonId);
            body.set("slot", slot);
            if (antiForgery) {
                body.set("__RequestVerificationToken", antiForgery);
            }

            const response = await fetch(slotUrl, {
                method: "POST",
                body: body,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) {
                let data = null;
                try { data = await response.json(); } catch (e) { /* JSON değil */ }
                if (data && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }
                throw new Error((data && data.error) || "Model yanıtı alınamadı (" + response.status + ").");
            }

            container.innerHTML = await response.text();
        } catch (err) {
            container.innerHTML = renderSlotErrorCard(
                slot,
                fallbackModelName || ("Model " + slot),
                err.message || "Bağlantı sırasında bir hata oluştu."
            );
        }
    }

    async function submitComparison() {
        const text = prompt.value.trim();
        if (!text) {
            showError("Karşılaştırma için bir prompt yazın.");
            return;
        }

        const modelAId = modelASelect ? modelASelect.value : "";
        const modelBId = modelBSelect ? modelBSelect.value : "";
        if (!modelAId || !modelBId) {
            showError("Lütfen iki model seçin.");
            return;
        }
        if (modelAId === modelBId) {
            showError("Karşılaştırma için iki farklı model seçmelisiniz.");
            return;
        }

        const rawTemp = tempInput ? tempInput.value.trim().replace(",", ".") : "";
        if (rawTemp !== "") {
            const t = Number(rawTemp);
            if (Number.isNaN(t) || t < 0 || t > 2) {
                showError("Temperature değeri 0 ile 2 arasında olmalıdır.");
                return;
            }
        }

        const rawMax = maxTokensInput ? maxTokensInput.value.trim() : "";
        if (rawMax !== "") {
            const m = Number(rawMax);
            if (!Number.isInteger(m) || m < 1 || m > 128000) {
                showError("Maksimum token 1 ile 128.000 arasında bir tam sayı olmalıdır.");
                return;
            }
        }

        const startUrl = form.dataset.compareStart;
        const slotUrl = form.dataset.compareSlot;
        if (!startUrl || !slotUrl) {
            form.action = form.dataset.compareAction;
            form.submit();
            return;
        }

        showError("");
        const modelAName = selectedOptionName(modelASelect);
        const modelBName = selectedOptionName(modelBSelect);
        const systemText = systemPromptInput ? systemPromptInput.value.trim() : "";

        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        const antiForgery = tokenInput ? tokenInput.value : "";

        const body = new URLSearchParams();
        body.set("ModelAId", modelAId);
        body.set("ModelBId", modelBId);
        body.set("UserPrompt", text);
        if (systemText) body.set("SystemPrompt", systemText);
        if (rawTemp !== "") body.set("Temperature", rawTemp);
        if (rawMax !== "") body.set("MaxTokens", rawMax);
        if (antiForgery) body.set("__RequestVerificationToken", antiForgery);

        // Prompt gönderildiği anda kutular ekranda belirsin, kutu boşalsın.
        root?.classList.add("is-battle-active");
        battleStage?.classList.remove("d-none");
        if (battlePromptText) battlePromptText.textContent = text;
        if (battleSystemNote) {
            battleSystemNote.textContent = systemText ? "System prompt: " + systemText : "";
            battleSystemNote.classList.toggle("d-none", !systemText);
        }
        if (slotAContainer) slotAContainer.innerHTML = renderLoadingCard("A", modelAName);
        if (slotBContainer) slotBContainer.innerHTML = renderLoadingCard("B", modelBName);

        prompt.value = "";
        autoResize();
        setBusy(true);

        try {
            const startRes = await fetch(startUrl, {
                method: "POST",
                body: body,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!startRes.ok) {
                let data = null;
                try { data = await startRes.json(); } catch (e) { /* JSON değil */ }
                if (data && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }
                throw new Error((data && data.error) || "Karşılaştırma başlatılamadı (" + startRes.status + ").");
            }

            const startData = await startRes.json();
            if (startData.resultUrl && window.history && window.history.replaceState) {
                window.history.replaceState({}, "", startData.resultUrl);
            }

            await Promise.all([
                runSlot(startData.comparisonId, "A", slotUrl, antiForgery, slotAContainer, modelAName),
                runSlot(startData.comparisonId, "B", slotUrl, antiForgery, slotBContainer, modelBName)
            ]);
        } catch (err) {
            battleStage?.classList.add("d-none");
            root?.classList.remove("is-battle-active");
            prompt.value = text;
            autoResize();
            showError(err.message || "Karşılaştırma başlatılırken bir hata oluştu.");
        } finally {
            setBusy(false);
            prompt.focus();
        }
    }

    modeButtons.forEach(function (btn) {
        btn.addEventListener("click", function () {
            setMode(btn.dataset.mode);
            prompt.focus();
        });
    });

    prompt.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            if (prompt.value.trim()) {
                form.requestSubmit();
            }
        }
    });

    prompt.addEventListener("input", autoResize);

    form.addEventListener("submit", function (e) {
        if (busy) {
            e.preventDefault();
            return;
        }

        if (getMode() === "compare") {
            e.preventDefault();
            submitComparison();
            return;
        }

        // Sohbet modu: mevcut form POST akışı (Chat/Create)
        if (form.dataset.chatAction) {
            form.action = form.dataset.chatAction;
        }
        setBusy(true);
    });

    setMode(getMode());
    prompt.focus();
})();
