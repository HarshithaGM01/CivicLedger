const state = {
  grants: [],
  dashboard: null
};

const elements = {
  rows: document.querySelector("#grantRows"),
  search: document.querySelector("#searchInput"),
  empty: document.querySelector("#emptyState"),
  grantDialog: document.querySelector("#grantDialog"),
  grantForm: document.querySelector("#grantForm"),
  grantFormError: document.querySelector("#grantFormError"),
  detailDialog: document.querySelector("#detailDialog"),
  detailContent: document.querySelector("#detailContent"),
  toast: document.querySelector("#toast")
};

const currency = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0
});

const shortDate = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "numeric",
  year: "numeric"
});

async function request(url, options = {}) {
  const response = await fetch(url, {
    headers: { "Content-Type": "application/json", ...options.headers },
    ...options
  });

  if (!response.ok) {
    const payload = await response.json().catch(() => ({}));
    const message = payload.error
      || Object.values(payload.errors || {}).flat()[0]
      || "The request could not be completed.";
    throw new Error(message);
  }

  return response.status === 204 ? null : response.json();
}

async function loadData() {
  setRefreshState(true);
  try {
    const [grants, dashboard] = await Promise.all([
      request("/api/grants"),
      request("/api/dashboard")
    ]);
    state.grants = grants;
    state.dashboard = dashboard;
    render();
  } catch (error) {
    showToast(error.message);
  } finally {
    setRefreshState(false);
  }
}

function render() {
  renderMetrics();
  renderGrants();
  renderDepartments();
  document.querySelector("#lastUpdated").textContent =
    `Updated ${new Date().toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })}`;
  refreshIcons();
}

function renderMetrics() {
  const dashboard = state.dashboard;
  document.querySelector("#totalAwarded").textContent = currency.format(dashboard.totalAwarded);
  document.querySelector("#totalSpent").textContent = currency.format(dashboard.totalSpent);
  document.querySelector("#activeGrants").textContent = dashboard.activeGrants;
  document.querySelector("#atRiskGrants").textContent = dashboard.atRiskGrants;
  document.querySelector("#grantCount").textContent = `${dashboard.totalGrants} total grants`;
  document.querySelector("#utilization").textContent = `${dashboard.utilizationPercent}% portfolio utilization`;
}

function renderGrants() {
  const query = elements.search.value.trim().toLowerCase();
  const grants = state.grants.filter(grant =>
    [grant.name, grant.department, grant.fundingSource, grant.status]
      .some(value => value.toLowerCase().includes(query)));

  elements.rows.innerHTML = grants.map(grant => `
    <tr>
      <td>
        <span class="grant-name">${escapeHtml(grant.name)}</span>
        <span class="grant-source">${escapeHtml(grant.fundingSource)}</span>
      </td>
      <td>${escapeHtml(grant.department)}</td>
      <td><span class="status status-${kebab(grant.status)}">${formatStatus(grant.status)}</span></td>
      <td>
        <div class="progress"><span style="width:${Math.min(grant.utilizationPercent, 100)}%"></span></div>
        ${grant.utilizationPercent}% of ${currency.format(grant.awardedAmount)}
      </td>
      <td>${formatDate(grant.endDate)}</td>
      <td><button class="text-button" data-view-grant="${grant.id}" type="button">View</button></td>
    </tr>
  `).join("");

  elements.empty.classList.toggle("hidden", grants.length > 0);
}

function renderDepartments() {
  document.querySelector("#departmentList").innerHTML = state.dashboard.departments
    .map(department => {
      const percent = department.awarded
        ? Math.round(department.spent / department.awarded * 100)
        : 0;
      return `
        <article class="department-item">
          <header>
            <strong>${escapeHtml(department.department)}</strong>
            <small>${department.grantCount} grant${department.grantCount === 1 ? "" : "s"}</small>
          </header>
          <div class="progress"><span style="width:${Math.min(percent, 100)}%"></span></div>
          <small>${currency.format(department.spent)} of ${currency.format(department.awarded)}</small>
        </article>
      `;
    }).join("");
}

async function openGrant(id) {
  try {
    const [grant, risk] = await Promise.all([
      request(`/api/grants/${id}`),
      request(`/api/grants/${id}/risk`)
    ]);
    elements.detailContent.innerHTML = detailTemplate(grant, risk);
    elements.detailDialog.showModal();
    refreshIcons();
  } catch (error) {
    showToast(error.message);
  }
}

function detailTemplate(grant, risk) {
  const canSpend = grant.status === "Active" || grant.status === "AtRisk";
  const canActivate = grant.status === "Draft";
  return `
    <div class="detail-header">
      <div>
        <p class="eyebrow">${escapeHtml(grant.department)}</p>
        <h2>${escapeHtml(grant.name)}</h2>
        <p>${escapeHtml(grant.fundingSource)} · ${formatDate(grant.startDate)} to ${formatDate(grant.endDate)}</p>
      </div>
      <button class="icon-button" data-close-detail type="button" title="Close" aria-label="Close">
        <i data-lucide="x"></i>
      </button>
    </div>
    <section class="detail-summary">
      <div><span>Awarded</span><strong>${currency.format(grant.awardedAmount)}</strong><small>${formatStatus(grant.status)}</small></div>
      <div><span>Spent</span><strong>${currency.format(grant.spentAmount)}</strong><small>${grant.utilizationPercent}% utilized</small></div>
      <div><span>Risk score</span><strong>${risk.score}/100</strong><small>${risk.level} risk</small></div>
    </section>
    <section class="detail-section">
      <h3>Risk signals</h3>
      <div class="audit-list">
        ${risk.reasons.map(reason => `<div class="audit-item">${escapeHtml(reason)}</div>`).join("")}
      </div>
    </section>
    ${canSpend ? expenseFormTemplate(grant.id) : ""}
    <section class="detail-section">
      <h3>Audit history</h3>
      <div class="audit-list">
        ${grant.auditEntries.map(entry => `
          <div class="audit-item">
            <strong>${escapeHtml(entry.action)}</strong>
            <span>${escapeHtml(entry.details)} · ${new Date(entry.createdAtUtc).toLocaleString()}</span>
          </div>
        `).join("")}
      </div>
    </section>
    <div class="dialog-actions">
      ${canActivate ? `<button class="primary-button" data-activate="${grant.id}" type="button"><i data-lucide="circle-play"></i>Activate grant</button>` : ""}
      <button class="secondary-button" data-close-detail type="button">Close</button>
    </div>
  `;
}

function expenseFormTemplate(id) {
  const today = new Date().toISOString().slice(0, 10);
  return `
    <section class="detail-section">
      <h3>Record expense</h3>
      <form class="expense-form" data-expense-form="${id}">
        <label class="span-2">Vendor<input name="vendor" required maxlength="120"></label>
        <label>Category<input name="category" required maxlength="80"></label>
        <label>Amount<input name="amount" type="number" min="0.01" step="0.01" required></label>
        <label>Date<input name="incurredOn" type="date" value="${today}" required></label>
        <button class="primary-button" type="submit"><i data-lucide="receipt-text"></i>Add expense</button>
      </form>
    </section>
  `;
}

async function createGrant(event) {
  event.preventDefault();
  elements.grantFormError.classList.add("hidden");
  const data = Object.fromEntries(new FormData(elements.grantForm));
  data.awardedAmount = Number(data.awardedAmount);

  try {
    await request("/api/grants", {
      method: "POST",
      body: JSON.stringify(data)
    });
    elements.grantDialog.close();
    elements.grantForm.reset();
    showToast("Grant created.");
    await loadData();
  } catch (error) {
    elements.grantFormError.textContent = error.message;
    elements.grantFormError.classList.remove("hidden");
  }
}

async function activateGrant(id) {
  try {
    await request(`/api/grants/${id}/activate`, { method: "POST" });
    elements.detailDialog.close();
    showToast("Grant activated.");
    await loadData();
  } catch (error) {
    showToast(error.message);
  }
}

async function addExpense(form) {
  const id = form.dataset.expenseForm;
  const data = Object.fromEntries(new FormData(form));
  data.amount = Number(data.amount);

  try {
    await request(`/api/grants/${id}/expenses`, {
      method: "POST",
      body: JSON.stringify(data)
    });
    showToast("Expense recorded and risk score refreshed.");
    await loadData();
    await openGrant(id);
  } catch (error) {
    showToast(error.message);
  }
}

function showToast(message) {
  elements.toast.textContent = message;
  elements.toast.classList.remove("hidden");
  clearTimeout(showToast.timeout);
  showToast.timeout = setTimeout(() => elements.toast.classList.add("hidden"), 3500);
}

function setRefreshState(isLoading) {
  const button = document.querySelector("#refreshButton");
  button.disabled = isLoading;
  button.classList.toggle("loading", isLoading);
}

function formatDate(value) {
  return shortDate.format(new Date(`${value}T00:00:00`));
}

function formatStatus(value) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function kebab(value) {
  return value.replace(/([a-z])([A-Z])/g, "$1-$2").toLowerCase();
}

function escapeHtml(value) {
  const element = document.createElement("div");
  element.textContent = value;
  return element.innerHTML;
}

function refreshIcons() {
  if (window.lucide) {
    window.lucide.createIcons();
  }
}

document.querySelector("#fiscalYear").textContent = new Date().getFullYear();
document.querySelector("#newGrantButton").addEventListener("click", () => elements.grantDialog.showModal());
document.querySelector("#refreshButton").addEventListener("click", loadData);
elements.search.addEventListener("input", renderGrants);
elements.grantForm.addEventListener("submit", createGrant);
document.querySelectorAll(".close-dialog").forEach(button =>
  button.addEventListener("click", () => elements.grantDialog.close()));

document.addEventListener("click", event => {
  const viewButton = event.target.closest("[data-view-grant]");
  const activateButton = event.target.closest("[data-activate]");
  if (viewButton) {
    openGrant(viewButton.dataset.viewGrant);
  }
  if (activateButton) {
    activateGrant(activateButton.dataset.activate);
  }
  if (event.target.closest("[data-close-detail]")) {
    elements.detailDialog.close();
  }
});

document.addEventListener("submit", event => {
  const expenseForm = event.target.closest("[data-expense-form]");
  if (expenseForm) {
    event.preventDefault();
    addExpense(expenseForm);
  }
});

loadData();
