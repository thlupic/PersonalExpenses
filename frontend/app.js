const API_BASE_URL = "https://localhost:7058/api";

const dateDisplay = document.getElementById("dateDisplay");
const descriptionList = document.getElementById("descriptionList");
const locationList = document.getElementById("locationList");
const expenseTypeSelect = document.getElementById("expenseType");

const form = document.getElementById("expenseForm");
const output = document.getElementById("output");
const message = document.getElementById("message");

let datePicker;

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
    let errorMessage = `Request failed: ${url} (${response.status})`;

    try {
      const errorData = await response.json();
      if (errorData.error) {
        errorMessage = errorData.error;
      }
    } catch {
      const text = await response.text();
      if (text) {
        errorMessage = text;
      }
    }

    throw new Error(errorMessage);
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
    showMessage(`Could not load lookup data: ${error.message}`, "error");
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
    let errorMessage = "Failed to save expense.";

    try {
      const errorData = await response.json();
      if (errorData.error) {
        errorMessage = errorData.error;
      }
    } catch {
      const text = await response.text();
      if (text) {
        errorMessage = text;
      }
    }

    throw new Error(errorMessage);
  }

  return response.json();
}

function initializeDatePicker() {
  datePicker = flatpickr("#dateDisplay", {
    dateFormat: "d/m/Y",
    defaultDate: "today",
    allowInput: false,
    locale: {
      firstDayOfWeek: 1
    }
  });
}

function getSelectedDateIso() {
  if (!datePicker || !datePicker.selectedDates || datePicker.selectedDates.length === 0) {
    return null;
  }

  return datePicker.selectedDates[0].toISOString().split("T")[0];
}

form.addEventListener("submit", async event => {
  event.preventDefault();

  const isoDate = getSelectedDateIso();

  if (!isoDate) {
    showMessage("Date is required.", "error");
    return;
  }

  const data = {
    date: isoDate,
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
    showMessage(`Could not save expense: ${error.message}`, "error");
  }
});

form.addEventListener("reset", () => {
  setTimeout(() => {
    if (datePicker) {
      datePicker.setDate(new Date(), true);
    }

    output.style.display = "none";
    output.textContent = "";
    showMessage("Form reset.", "info");
  }, 0);
});

initializeDatePicker();
loadLookups();