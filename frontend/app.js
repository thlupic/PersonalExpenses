const API_BASE_URL = "http://localhost:5180/api";

const dateInput = document.getElementById("date");
const dateDisplay = document.getElementById("dateDisplay");

const descriptionList = document.getElementById("descriptionList");
const locationList = document.getElementById("locationList");
const expenseTypeSelect = document.getElementById("expenseType");

const form = document.getElementById("expenseForm");
const output = document.getElementById("output");
const message = document.getElementById("message");

function formatDateToDisplay(isoDate) {
  const [year, month, day] = isoDate.split("-");
  return `${day}/${month}/${year}`;
}

function setToday() {
  const today = new Date();
  const yyyy = today.getFullYear();
  const mm = String(today.getMonth() + 1).padStart(2, "0");
  const dd = String(today.getDate()).padStart(2, "0");

  const isoDate = `${yyyy}-${mm}-${dd}`;
  dateInput.value = isoDate;
  dateDisplay.value = formatDateToDisplay(isoDate);
}

function showMessage(text, type = "info") {
  message.textContent = text;
  message.className = `message ${type}`;
}

function fillDataList(dataListElement, items) {
  dataListElement.innerHTML = "";

  items.forEach(item => {
    const option = document.createElement("option");
    option.value = item;
    dataListElement.appendChild(option);
  });
}

function fillSelect(selectElement, items) {
  selectElement.innerHTML = "";

  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = "Select expense type";
  selectElement.appendChild(defaultOption);

  items.forEach(item => {
    const option = document.createElement("option");
    option.value = item;
    option.textContent = item;
    selectElement.appendChild(option);
  });
}

async function fetchJson(url) {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Request failed: ${url} (${response.status})`);
  }

  return response.json();
}

async function loadLookups() {
  try {
    const [descriptions, locations, expenseTypes] = await Promise.all([
      fetchJson(`${API_BASE_URL}/lookups/descriptions`),
      fetchJson(`${API_BASE_URL}/lookups/locations`),
      fetchJson(`${API_BASE_URL}/lookups/expense-types`)
    ]);

    fillDataList(descriptionList, descriptions);
    fillDataList(locationList, locations);
    fillSelect(expenseTypeSelect, expenseTypes);

    showMessage("Lookup data loaded successfully.", "info");
  } catch (error) {
    console.error(error);
    showMessage(error.message, "error");
  }
}

async function saveExpense(data) {
  const response = await fetch(`${API_BASE_URL}/expenses`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(data)
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || "Failed to save expense.");
  }

  return response.json();
}

dateDisplay.addEventListener("click", () => {
  if (typeof dateInput.showPicker === "function") {
    dateInput.showPicker();
  } else {
    dateInput.focus();
    dateInput.click();
  }
});

dateInput.addEventListener("change", () => {
  if (dateInput.value) {
    dateDisplay.value = formatDateToDisplay(dateInput.value);
  }
});

form.addEventListener("submit", async event => {
  event.preventDefault();

  const data = {
    date: dateInput.value,
    description: document.getElementById("description").value.trim(),
    location: document.getElementById("location").value.trim(),
    quantity: parseFloat(document.getElementById("quantity").value),
    expenseType: expenseTypeSelect.value,
    amount: parseFloat(document.getElementById("amount").value)
  };

  try {
    const saved = await saveExpense(data);

    output.style.display = "block";
    output.textContent = JSON.stringify(saved, null, 2);

    showMessage(`Expense saved successfully with ID ${saved.id}.`, "info");
  } catch (error) {
    console.error(error);
    showMessage(error.message, "error");
  }
});

form.addEventListener("reset", () => {
  setTimeout(() => {
    setToday();
    output.style.display = "none";
    output.textContent = "";
    showMessage("Form reset.", "info");
  }, 0);
});

setToday();
loadLookups();