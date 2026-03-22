const dateInput = document.getElementById("date");
const dateDisplay = document.getElementById("dateDisplay");

const descriptionList = document.getElementById("descriptionList");
const locationList = document.getElementById("locationList");
const expenseTypeSelect = document.getElementById("expenseType");

const form = document.getElementById("expenseForm");
const message = document.getElementById("message");
const output = document.getElementById("output");

const API_URL = "http://localhost:5180/api/expenses";

/* ---------------- DATE HANDLING ---------------- */

function formatDateToDisplay(dateString) {
  const [year, month, day] = dateString.split("-");
  return `${day}/${month}/${year}`;
}

function setToday() {
  const today = new Date();
  const yyyy = today.getFullYear();
  const mm = String(today.getMonth() + 1).padStart(2, "0");
  const dd = String(today.getDate()).padStart(2, "0");

  const iso = `${yyyy}-${mm}-${dd}`;
  dateInput.value = iso;
  dateDisplay.value = formatDateToDisplay(iso);
}

dateDisplay.addEventListener("click", () => {
  dateInput.showPicker(); // opens native picker
});

dateInput.addEventListener("change", () => {
  dateDisplay.value = formatDateToDisplay(dateInput.value);
});

/* ---------------- UI HELPERS ---------------- */

function showMessage(text, type = "info") {
  message.textContent = text;
  message.className = `message ${type}`;
}

/* ---------------- JSON LOADING ---------------- */

function normalizeArray(data) {
  if (Array.isArray(data)) return data;
  if (data && Array.isArray(data.items)) return data.items;
  return [];
}

function fillDataList(element, items) {
  element.innerHTML = "";

  items.forEach(item => {
    const option = document.createElement("option");
    option.value = typeof item === "string"
      ? item
      : item.name ?? item.value ?? "";

    if (option.value) element.appendChild(option);
  });
}

function fillSelect(element, items) {
  element.innerHTML = '<option value="">Select...</option>';

  items.forEach(item => {
    const value = typeof item === "string"
      ? item
      : item.name ?? item.value ?? "";

    if (!value) return;

    const option = document.createElement("option");
    option.value = value;
    option.textContent = value;
    element.appendChild(option);
  });
}

async function loadJson(file) {
  const res = await fetch(file);

  if (!res.ok) {
    throw new Error(`Failed to load ${file} - HTTP ${res.status}`);
  }

  try {
    return await res.json();
  } catch (error) {
    throw new Error(`Invalid JSON in ${file}`);
  }
}

async function loadLookups() {
  try {
    const [expenses, locations, types] = await Promise.all([
      loadJson("expenses.json"),
      loadJson("locations.json"),
      loadJson("expense_type.json")
    ]);

    fillDataList(descriptionList, normalizeArray(expenses));
    fillDataList(locationList, normalizeArray(locations));
    fillSelect(expenseTypeSelect, normalizeArray(types));

    showMessage("Lookups loaded.");
  } catch (err) {
	  console.error(err);
	  showMessage(err.message, "error");
  }
}

/* ---------------- API ---------------- */

async function saveExpense(data) {
  const res = await fetch(API_URL, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(data)
  });

  if (!res.ok) throw new Error("Save failed");
  return res.json();
}

/* ---------------- FORM ---------------- */

form.addEventListener("submit", async (e) => {
  e.preventDefault();

  const data = {
    date: dateInput.value, // ISO sent to backend
    description: document.getElementById("description").value,
    location: document.getElementById("location").value,
    quantity: parseFloat(document.getElementById("quantity").value),
    expenseType: expenseTypeSelect.value,
    amount: parseFloat(document.getElementById("amount").value)
  };

  try {
    const saved = await saveExpense(data);

    output.style.display = "block";
    output.textContent = JSON.stringify(saved, null, 2);

    showMessage(`Saved (ID: ${saved.id})`);
  } catch {
    showMessage("Save failed", "error");
  }
});

form.addEventListener("reset", () => {
  setTimeout(() => {
    setToday();
    output.style.display = "none";
    output.textContent = "";
    showMessage("Form reset");
  }, 0);
});

/* ---------------- INIT ---------------- */

setToday();
loadLookups();