const money = new Intl.NumberFormat('uk-UA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const dateFmt = new Intl.DateTimeFormat('uk-UA', { dateStyle: 'short', timeStyle: 'short' });

let selectedGoodId = null;

// Помилка, що прийшла з API (у тому числі виключення, згенероване в SQL Server)
class ApiError extends Error {
  constructor(problem, status) {
    super(problem.detail || problem.title || `HTTP ${status}`);
    this.status = status;
    this.sqlErrorNumber = problem.sqlErrorNumber;
    this.procedure = problem.procedure;
  }
}

async function api(url, options = {}) {
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  const body = res.status === 204 ? null : await res.json().catch(() => ({}));
  if (!res.ok) throw new ApiError(body || {}, res.status);
  return body;
}

const esc = (v) => String(v ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const fmtMoney = (v) => (v == null ? '—' : money.format(v));

// 1. Товари
async function loadGoods() {
  const goods = await api('/api/goods');
  const tbody = document.querySelector('#goods-table tbody');
  tbody.innerHTML = goods.map((g) => `
    <tr data-id="${g.goodId}" class="${g.goodId === selectedGoodId ? 'selected' : ''}">
      <td>${g.goodId}</td>
      <td>${esc(g.name)}</td>
      <td>${esc(g.departmentName)}</td>
      <td>${esc(g.producer)}</td>
      <td class="num">${fmtMoney(g.price)}</td>
      <td class="num">${g.quantity ?? '—'}</td>
    </tr>`).join('');
}

document.querySelector('#goods-table tbody').addEventListener('click', (e) => {
  const row = e.target.closest('tr[data-id]');
  if (!row) return;
  selectedGoodId = Number(row.dataset.id);
  document.querySelectorAll('#goods-table tbody tr').forEach((r) => r.classList.toggle('selected', r === row));
  document.getElementById('sale-result').hidden = true;
  loadDetails(selectedGoodId);
});

async function loadDetails(id) {
  const { good, sales, totalSold } = await api(`/api/goods/${id}`);
  const section = document.getElementById('details-section');
  section.hidden = false;

  document.getElementById('d-name').textContent = good.name;
  document.getElementById('d-props').innerHTML = `
    <dt>Код</dt><dd>${good.goodId}</dd>
    <dt>Відділ</dt><dd>${esc(good.departmentName)}</dd>
    <dt>Виробник</dt><dd>${esc(good.producer)}</dd>
    <dt>Ціна</dt><dd>${fmtMoney(good.price)} грн</dd>
    <dt>На складі</dt><dd>${good.quantity ?? '—'} шт.</dd>
    <dt>Опис</dt><dd>${esc(good.description) || '—'}</dd>`;

  document.querySelector('#sales-table tbody').innerHTML = sales.length
    ? sales.map((s) => `
      <tr>
        <td>${s.salesId}</td>
        <td>${s.checkNo}</td>
        <td class="num">${s.quantity}</td>
        <td>${s.saleDate ? dateFmt.format(new Date(s.saleDate)) : '—'}</td>
      </tr>`).join('')
    : '<tr><td colspan="4" class="empty">Продажів ще не було</td></tr>';
  document.getElementById('d-total').textContent = totalSold;

  if (!document.querySelector('#sale-form [name=checkNo]').value) {
    document.querySelector('#sale-form [name=checkNo]').value = 20000 + Math.floor(Math.random() * 9000);
  }
  section.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// 2. Виконання процедури та 4. оброблення виключення з бази даних
document.getElementById('sale-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const form = new FormData(e.target);
  const result = document.getElementById('sale-result');

  try {
    const { salesId } = await api('/api/sales', {
      method: 'POST',
      body: JSON.stringify({
        goodId: selectedGoodId,
        quantity: Number(form.get('quantity')),
        checkNo: Number(form.get('checkNo')),
      }),
    });
    showAlert(result, 'ok', `Продаж №${salesId} успішно оформлено.`);
    await Promise.all([loadGoods(), loadDetails(selectedGoodId)]);
  } catch (err) {
    const source = err.sqlErrorNumber
      ? `<small>SQL Server, помилка ${err.sqlErrorNumber}${err.procedure ? `, процедура ${esc(err.procedure)}` : ''}</small>`
      : '';
    showAlert(result, 'error', `${esc(err.message)}${source}`);
  }
});

function showAlert(el, kind, html) {
  el.className = `alert ${kind}`;
  el.innerHTML = html;
  el.hidden = false;
}

// 3. Скалярна та таблична функції
async function loadDepartments() {
  const depts = await api('/api/departments');
  document.getElementById('dept-select').innerHTML = depts
    .map((d) => `<option value="${d.deptId}">${esc(d.departmentName)}</option>`).join('');
}

document.getElementById('func-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const form = new FormData(e.target);
  const deptId = form.get('deptId');
  const minPrice = form.get('minPrice') || 0;

  const [count, goods] = await Promise.all([
    api(`/api/departments/${deptId}/goods-count`),
    api(`/api/departments/${deptId}/goods?minPrice=${encodeURIComponent(minPrice)}`),
  ]);

  document.getElementById('scalar-result').textContent = count.goodsCount;
  document.querySelector('#tvf-table tbody').innerHTML = goods.length
    ? goods.map((g) => `
      <tr>
        <td>${g.goodId}</td>
        <td>${esc(g.name)}</td>
        <td>${esc(g.producer)}</td>
        <td class="num">${fmtMoney(g.price)}</td>
        <td class="num">${g.quantity ?? '—'}</td>
        <td class="num">${g.soldQuantity}</td>
      </tr>`).join('')
    : '<tr><td colspan="6" class="empty">Немає товарів за заданими умовами</td></tr>';
});

loadGoods();
loadDepartments();
