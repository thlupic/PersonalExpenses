const API_BASE_URL =
  window.APP_CONFIG && window.APP_CONFIG.API_BASE_URL
    ? window.APP_CONFIG.API_BASE_URL.replace(/\/$/, "")
    : "/api";

const dateDisplay = document.getElementById("dateDisplay");
const descriptionList = document.getElementById("descriptionList");
const locationList = document.getElementById("locationList");
const expenseTypeSelect = document.getElementById("expenseType");

const form = document.getElementById("expenseForm");
const output = document.getElementById("output");
const message = document.getElementById("message");

let datePicker;

function apiUrl(path) {
  return `${API_BASE_URL}${path}`;
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

async function readErrorResponse(response, fallbackMessage) {
  try {
    const contentType = response.headers.get("content-type") || "";

    if (contentType.includes("application/json")) {
      const errorData = await response.json();
      if (errorData.error) {
        return errorData.error;
      }
    }

    const text = await response.text();
    if (text) {
      return text;
    }
  } catch {
  }

  return fallbackMessage;
}

async function fetchJson(url, options = {}) {
  const response = await fetch(url, options);

  if (!response.ok) {
    const errorMessage = await readErrorResponse(
      response,
      `Request failed: ${response.status}`
    );

    throw new Error(errorMessage);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

async function loadLookups() {
  try {
    const [descriptions, locations, expenseTypes] = await Promise.all([
      fetchJson(apiUrl("/lookups/descriptions")),
      fetchJson(apiUrl("/lookups/locations")),
      fetchJson(apiUrl("/lookups/expense-types"))
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
  return fetchJson(apiUrl("/expenses"), {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(data)
  });
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

  const selectedDate = datePicker.selectedDates[0];
  const year = selectedDate.getFullYear();
  const month = String(selectedDate.getMonth() + 1).padStart(2, "0");
  const day = String(selectedDate.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
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
    form.reset();

    if (datePicker) {
      datePicker.setDate(new Date(), true);
    }
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