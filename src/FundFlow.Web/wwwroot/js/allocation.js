// Fondsaufteilung im Änderungsformular: Zeilen hinzufügen und entfernen ohne Neuladen,
// laufende Summe der Anteile (US-02, Kriterium 3). Ohne JavaScript übernehmen die
// Server-Handler „AddRow“ und „RemoveRow“ dieselben Aufgaben; geprüft wird immer serverseitig.
(() => {
  const list = document.querySelector("[data-allocation-rows]");
  if (!list) return;

  const template = document.querySelector("[data-allocation-template]");
  const addButton = document.querySelector("[data-add-row]");
  const sum = document.querySelector("[data-sum]");
  const sumValue = document.querySelector("[data-sum-value]");
  const maxRows = Number(list.dataset.maxRows || 6);
  const format = new Intl.NumberFormat("de-DE", { maximumFractionDigits: 2 });

  const rows = () => [...list.querySelectorAll("[data-allocation-row]")];

  // Feldnamen fortlaufend halten, damit die Liste beim Absenden lückenlos gebunden wird.
  const reindex = () => {
    rows().forEach((row, i) => {
      row.querySelectorAll("[data-field]").forEach((field) => {
        const kind = field.dataset.field === "InstrumentId" ? "fund" : "pct";
        field.name = `Form.Allocations[${i}].${field.dataset.field}`;
        field.id = `f-alloc-${i}-${kind}`;
        const label = field.parentElement.querySelector("label");
        if (label) {
          label.htmlFor = field.id;
          label.textContent = kind === "fund" ? `Fonds ${i + 1}` : `Anteil Fonds ${i + 1} in Prozent`;
        }
      });
      const remove = row.querySelector("[data-remove-row]");
      remove.value = String(i);
      remove.setAttribute("aria-label", `Fonds ${i + 1} entfernen`);
    });
    if (addButton) addButton.hidden = rows().length >= maxRows;
  };

  const updateSum = () => {
    let total = 0;
    let readable = true;
    list.querySelectorAll("[data-percentage]").forEach((input) => {
      const text = input.value.trim().replace(/\s*%$/, "");
      if (text === "") return;
      if (!/^-?\d{1,9}(,\d+)?$/.test(text)) { readable = false; return; }
      total += Number(text.replace(",", "."));
    });
    sumValue.textContent = readable ? `${format.format(total)} %` : "–";
    sum.classList.remove("sum--ok", "sum--off", "sum--unknown");
    sum.classList.add(!readable ? "sum--unknown" : total === 100 ? "sum--ok" : "sum--off");
  };

  addButton?.addEventListener("click", (event) => {
    event.preventDefault();
    if (rows().length >= maxRows) return;
    list.append(template.content.cloneNode(true));
    reindex();
    rows().at(-1).querySelector("select").focus();
  });

  list.addEventListener("click", (event) => {
    const button = event.target.closest("[data-remove-row]");
    if (!button) return;
    event.preventDefault();
    const row = button.closest("[data-allocation-row]");
    const next = row.nextElementSibling ?? row.previousElementSibling;
    row.remove();
    reindex();
    updateSum();
    (next?.querySelector("select") ?? addButton)?.focus();
  });

  list.addEventListener("input", updateSum);

  document.querySelector("[data-focus-on-load]")?.focus();
  reindex();
  updateSum();
})();
