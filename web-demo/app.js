const STORAGE_KEY = 'gymmanager_demo_v2';
const root = document.getElementById('viewRoot');
const isoDate = date => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
const today = () => isoDate(new Date());
const fromISO = value => value ? new Date(`${value}T12:00:00`) : null;
const dateOffset = days => { const date = new Date(); date.setHours(12, 0, 0, 0); date.setDate(date.getDate() + days); return isoDate(date); };
const dateTimeOffset = (days, hour) => { const date = fromISO(dateOffset(days)); date.setHours(hour, 0, 0, 0); return date.toISOString(); };
const safe = value => String(value ?? '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]);
const money = value => new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(value || 0);
const dateLabel = value => value ? new Intl.DateTimeFormat('es-AR').format(fromISO(value)) : 'Sin pagos';
const fullName = member => `${member.nombre} ${member.apellido}`.trim();
const months = Array.from({ length: 12 }, (_, index) => new Intl.DateTimeFormat('es-AR', { month: 'long' }).format(new Date(2026, index, 1)));

function sampleData() {
    const socios = [
        { id: 1, nombre: 'Juan', apellido: 'Pérez', dni: '28451632', telefono: '11 4567-1020', codigo: '0001234567', alta: dateOffset(-148), vencimiento: dateOffset(12), activo: true },
        { id: 2, nombre: 'María', apellido: 'Gómez', dni: '31908742', telefono: '11 5621-3340', codigo: '0009876543', alta: dateOffset(-95), vencimiento: dateOffset(-4), activo: true },
        { id: 3, nombre: 'Lucía', apellido: 'Fernández', dni: '33714208', telefono: '11 4432-8891', codigo: '0003388122', alta: dateOffset(-61), vencimiento: dateOffset(0), activo: true },
        { id: 4, nombre: 'Bruno', apellido: 'Díaz', dni: '30118904', telefono: '11 6012-7734', codigo: '0004500199', alta: dateOffset(-210), vencimiento: dateOffset(-12), activo: true },
        { id: 5, nombre: 'Sofía', apellido: 'Acosta', dni: '40551239', telefono: '11 5410-1198', codigo: '0005522001', alta: dateOffset(-34), vencimiento: dateOffset(3), activo: true },
        { id: 6, nombre: 'Diego', apellido: 'Romero', dni: '36771290', telefono: '11 6123-8877', codigo: '0006102450', alta: dateOffset(-9), vencimiento: '', activo: true },
        { id: 7, nombre: 'Paula', apellido: 'Ríos', dni: '29110438', telefono: '11 4770-2233', codigo: '0007726120', alta: dateOffset(-320), vencimiento: dateOffset(25), activo: false },
        { id: 8, nombre: 'Mateo', apellido: 'Suárez', dni: '42100831', telefono: '11 4981-0021', codigo: '0008893410', alta: dateOffset(-18), vencimiento: dateOffset(22), activo: true }
    ];
    return {
        socios,
        pagos: [
            { id: 1042, socioId: 1, fechaPago: dateTimeOffset(-1, 9), monto: 35000, montoTotal: 35000, forma: 'Transferencia', desde: dateOffset(-18), hasta: dateOffset(12) },
            { id: 1041, socioId: 5, fechaPago: dateTimeOffset(-2, 11), monto: 35000, montoTotal: 35000, forma: 'Efectivo', desde: dateOffset(-27), hasta: dateOffset(3) },
            { id: 1040, socioId: 3, fechaPago: dateTimeOffset(-5, 18), monto: 35000, montoTotal: 35000, forma: 'Débito / Crédito', desde: dateOffset(-30), hasta: dateOffset(0) },
            { id: 1039, socioId: 2, fechaPago: dateTimeOffset(-35, 10), monto: 35000, montoTotal: 35000, forma: 'Efectivo', desde: dateOffset(-65), hasta: dateOffset(-4) },
            { id: 1038, socioId: 8, fechaPago: dateTimeOffset(-7, 12), monto: 0, montoTotal: 35000, forma: 'Bonificación / Regalo', desde: dateOffset(-8), hasta: dateOffset(22) }
        ],
        asistencias: [
            { id: 1, socioId: 5, fechaHora: dateTimeOffset(0, 8) },
            { id: 2, socioId: 3, fechaHora: dateTimeOffset(0, 9) },
            { id: 3, socioId: 1, fechaHora: dateTimeOffset(-1, 18) },
            { id: 4, socioId: 8, fechaHora: dateTimeOffset(-1, 19) },
            { id: 5, socioId: 2, fechaHora: dateTimeOffset(-2, 10) }
        ],
        config: { nombre: 'Sucursal Central', telefono: '11 4000-2211', direccion: 'Av. San Martín 1250', precio: 35000, tolerancia: 3 },
        nextSocio: 9, nextPago: 1043, nextAsistencia: 6
    };
}

function loadData() {
    try {
        const saved = JSON.parse(localStorage.getItem(STORAGE_KEY));
        if (saved?.socios && saved?.pagos && saved?.asistencias && saved?.config) return saved;
    } catch { /* Restablece los datos de muestra si el almacenamiento no es válido. */ }
    return sampleData();
}

let data = loadData();
let view = 'inicio';
let accessResult = null;
let paymentFilter = { desde: `${today().slice(0, 8)}01`, hasta: today(), forma: 'Todos' };
let reportPeriod = { mes: new Date().getMonth(), anio: new Date().getFullYear() };
let toastTimer;
const save = () => localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
const activeMembers = () => data.socios.filter(member => !member.eliminado && member.activo);
const getMember = id => data.socios.find(member => member.id === Number(id));
const debtors = () => activeMembers().filter(member => !member.vencimiento || member.vencimiento < today());

function status(member) {
    if (!member.activo) return 'Inactivo';
    if (!member.vencimiento) return 'Sin pagos';
    if (member.vencimiento >= today()) return 'Al día';
    const daysLate = Math.floor((fromISO(today()) - fromISO(member.vencimiento)) / 86400000);
    return daysLate <= Number(data.config.tolerancia) ? 'En tolerancia' : 'Vencido';
}

function badge(text) {
    const color = text === 'Al día' ? 'success' : text === 'En tolerancia' ? 'warning' : text === 'Inactivo' ? 'neutral' : 'danger';
    return `<span class="badge ${color}">${safe(text)}</span>`;
}

function header(title, subtitle, action = '') {
    return `<header class="page-header"><div><span class="eyebrow">GYMMANAGER / ${safe(title.toUpperCase())}</span><h1>${safe(title)}</h1><p>${safe(subtitle)}</p></div>${action ? `<div class="page-actions">${action}</div>` : ''}</header>`;
}

function table(headings, rows, empty = 'No hay registros para mostrar.') {
    return `<div class="table-wrap"><table class="data-table"><thead><tr>${headings.map(item => `<th>${item}</th>`).join('')}</tr></thead><tbody>${rows || `<tr><td colspan="${headings.length}" class="empty-cell">${empty}</td></tr>`}</tbody></table></div>`;
}

function personRows(list, debtorMode = false) {
    return list.map(member => `<tr><td><div class="person-cell"><span class="avatar">${safe(member.nombre.slice(0, 1) + member.apellido.slice(0, 1))}</span><div><strong>${safe(fullName(member))}</strong><small>ID #${member.id} · DNI ${safe(member.dni)}</small></div></div></td><td>${safe(member.telefono || 'Sin registrar')}</td><td>${dateLabel(member.alta)}</td><td>${dateLabel(member.vencimiento)}</td><td>${badge(status(member))}</td><td class="row-actions"><button class="text-button" data-action="pay" data-id="${member.id}">${debtorMode ? 'Regularizar' : 'Cobrar'}</button>${debtorMode ? '' : `<button class="text-button" data-action="edit" data-id="${member.id}">Editar</button><button class="text-button muted-action" data-action="toggle" data-id="${member.id}">${member.activo ? 'Pausar' : 'Activar'}</button>`}</td></tr>`).join('');
}

function paymentRows(list) {
    return list.map(payment => {
        const member = getMember(payment.socioId);
        return `<tr><td class="mono">#${payment.id}</td><td>${new Intl.DateTimeFormat('es-AR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(payment.fechaPago))}</td><td><strong>${safe(member ? fullName(member) : 'Socio')}</strong></td><td class="amount">${money(payment.monto)}</td><td><span class="method-tag">${safe(payment.forma)}</span></td><td>${dateLabel(payment.desde)} – ${dateLabel(payment.hasta)}</td></tr>`;
    }).join('');
}

function attendanceRows(list) {
    return list.slice().sort((a, b) => new Date(b.fechaHora) - new Date(a.fechaHora)).map(item => {
        const member = getMember(item.socioId);
        return `<tr><td class="mono">${new Intl.DateTimeFormat('es-AR', { hour: '2-digit', minute: '2-digit' }).format(new Date(item.fechaHora))}</td><td>#${item.socioId}</td><td><strong>${safe(member ? fullName(member) : 'Socio')}</strong></td><td>${dateLabel(member?.vencimiento)}</td><td>${member ? badge(status(member)) : ''}</td></tr>`;
    }).join('');
}

function renderHome() {
    const todayEntries = data.asistencias.filter(item => item.fechaHora.slice(0, 10) === today());
    const newMembers = data.socios.filter(member => !member.eliminado && member.alta.slice(0, 7) === today().slice(0, 7)).length;
    const upcoming = activeMembers().filter(member => member.vencimiento >= today()).sort((a, b) => a.vencimiento.localeCompare(b.vencimiento)).slice(0, 5);
    return `${header('Panel de Control', 'Resumen de actividad diaria, concurrencia y control de membresías', `<span class="date-chip">${new Intl.DateTimeFormat('es-AR', { dateStyle: 'full' }).format(new Date())}</span>`)}
        <section class="metric-grid"><article class="metric"><span>Socios activos</span><strong class="metric-green">${activeMembers().length}</strong><small>Inscritos y habilitados</small></article><article class="metric"><span>Asistencias hoy</span><strong class="metric-blue">${todayEntries.length}</strong><small>Check-ins realizados hoy</small></article><article class="metric"><span>En mora / vencidas</span><strong class="metric-red">${debtors().length}</strong><small>Requieren regularización</small></article><article class="metric"><span>Altas del mes</span><strong class="metric-amber">${newMembers}</strong><small>Nuevos socios este mes</small></article></section>
        <section class="dashboard-grid"><article class="panel"><div class="panel-heading"><div><h2>Últimos ingresos de hoy</h2><p>${todayEntries.length} ingresos registrados</p></div><button class="text-button" data-view="acceso">Ver control de acceso</button></div>${table(['Hora', 'ID', 'Socio', 'Vencimiento', 'Estado'], attendanceRows(todayEntries), 'Todavía no hay ingresos registrados hoy.')}</article><article class="panel"><div class="panel-heading"><div><h2>Próximos vencimientos</h2><p>Socios con cuota vigente</p></div><button class="text-button" data-view="socios">Ver padrón</button></div>${table(['Vence', 'Socio', 'Teléfono'], upcoming.map(member => `<tr><td class="warning-text">${dateLabel(member.vencimiento)}</td><td><strong>${safe(fullName(member))}</strong></td><td>${safe(member.telefono)}</td></tr>`).join(''), 'No hay próximos vencimientos.')}</article></section>
        <section class="panel quick-panel"><div><h2>Accesos rápidos</h2><p>Atajos a las tareas habituales del gimnasio.</p></div><div class="quick-actions"><button class="button-primary" data-action="new-member">+ Nuevo socio</button><button class="button-secondary" data-view="acceso">Registrar asistencia</button><button class="button-secondary" data-action="pay">Registrar cobro</button><button class="button-secondary" data-view="reportes">Ver reportes</button></div></section>`;
}

function renderMembers() {
    const list = data.socios.filter(member => !member.eliminado);
    return `${header('Padrón de Socios', 'Alta, seguimiento de membresías, historial de pagos y vencimientos', '<button class="button-primary" data-action="new-member">+ Nuevo socio</button>')}<section class="panel"><div class="toolbar"><label class="search-field"><span>⌕</span><input id="memberSearch" type="search" placeholder="Buscar por nombre, apellido, DNI o ID" autocomplete="off"></label><span id="memberCount" class="result-count">${list.length} socios</span></div>${table(['Socio', 'Teléfono', 'Alta', 'Vencimiento', 'Cuota', 'Acciones'], personRows(list))}</section>`;
}

function renderAccess() {
    const result = accessResult || { state: 'idle', title: 'Esperando ingreso', detail: 'Pase un carnet, acerque el llavero o ingrese el ID / DNI del socio.' };
    const entries = data.asistencias.filter(item => item.fechaHora.slice(0, 10) === today());
    const sampleMembers = activeMembers().filter(member => member.codigo).slice(0, 4);
    return `${header('Control de Acceso', 'Registro de ingresos por ID, DNI o código de acceso. El lector físico se simula en esta demo.')}
        <section class="access-layout"><article class="panel access-terminal"><div class="terminal-heading"><span class="terminal-icon">⇥</span><div><h2>Terminal de ingreso</h2><p>Acceso sujeto al estado de la membresía</p></div></div><form data-form="access"><label for="accessCode">N° de socio, DNI o código de acceso</label><div class="input-action"><input id="accessCode" name="identifier" placeholder="Ej. 0001234567" autocomplete="off" required><button class="button-primary">Registrar entrada</button></div><small>Probá con una credencial de muestra:</small><div class="sample-credentials">${sampleMembers.map(member => `<button type="button" class="credential" data-action="quick-access" data-code="${safe(member.codigo)}"><strong>${safe(fullName(member))}</strong><span>${safe(member.codigo)}</span></button>`).join('')}</div></form><div class="access-result ${result.state}"><span class="result-light"></span><div><strong>${safe(result.title)}</strong><p>${safe(result.detail)}</p></div></div></article>
        <article class="panel attendance-panel"><div class="panel-heading"><div><h2>Ingresos registrados hoy</h2><p>${entries.length} ingresos registrados hoy</p></div><span id="liveClock" class="live-clock"></span></div>${table(['Hora', 'N° socio', 'Socio', 'Vencimiento', 'Estado'], attendanceRows(entries), 'Todavía no hay ingresos registrados hoy.')}</article></section>`;
}

function renderDebtors() {
    const list = debtors().sort((a, b) => (a.vencimiento || '').localeCompare(b.vencimiento || ''));
    return `${header('Gestión de Deudores y Vencidos', 'Socios activos con cuotas vencidas o sin pagos registrados.', `<span class="debt-counter"><strong>${list.length}</strong><span>socios en mora</span></span>`)}<section class="panel"><div class="toolbar"><label class="search-field"><span>⌕</span><input id="debtorSearch" type="search" placeholder="Buscar por nombre o DNI" autocomplete="off"></label><button class="button-secondary" data-action="refresh">Actualizar lista</button></div>${table(['Socio', 'Teléfono', 'Vencimiento', 'Atraso', 'Condición', 'Acción'], debtorRows(list))}</section>`;
}

function debtorRows(list) {
    return list.map(member => {
        const days = member.vencimiento ? Math.max(0, Math.floor((fromISO(today()) - fromISO(member.vencimiento)) / 86400000)) : null;
        return `<tr><td><div class="person-cell"><span class="avatar avatar-red">${safe(member.nombre.slice(0, 1) + member.apellido.slice(0, 1))}</span><div><strong>${safe(fullName(member))}</strong><small>DNI ${safe(member.dni)}</small></div></div></td><td>${safe(member.telefono || 'Sin registrar')}</td><td>${dateLabel(member.vencimiento)}</td><td>${days === null ? 'Sin cuota inicial' : `<span class="badge danger">${days} ${days === 1 ? 'día' : 'días'}</span>`}</td><td>${badge(status(member))}</td><td><button class="text-button" data-action="pay" data-id="${member.id}">Regularizar</button></td></tr>`;
    }).join('');
}

function filteredPayments() {
    return data.pagos.filter(payment => {
        const date = payment.fechaPago.slice(0, 10);
        return (!paymentFilter.desde || date >= paymentFilter.desde) && (!paymentFilter.hasta || date <= paymentFilter.hasta) && (paymentFilter.forma === 'Todos' || payment.forma === paymentFilter.forma);
    }).sort((a, b) => new Date(b.fechaPago) - new Date(a.fechaPago));
}

function renderPayments() {
    const list = filteredPayments();
    const total = list.reduce((sum, payment) => sum + Number(payment.monto), 0);
    const methods = ['Todos', 'Efectivo', 'Transferencia', 'Débito / Crédito', 'Bonificación / Regalo'];
    return `${header('Caja y Cobranzas', 'Auditoría de ingresos, historial de cobros y conciliación de caja', '<button class="button-primary" data-action="pay">+ Registrar cobro</button>')}<section class="payment-summary"><div><span>TOTAL RECAUDADO</span><strong>${money(total)}</strong><small>${list.length} cobros según los filtros</small></div><span class="summary-mark">$</span></section>
        <section class="panel"><div class="filter-bar"><label>Fecha desde<input type="date" data-filter="desde" value="${safe(paymentFilter.desde)}"></label><label>Fecha hasta<input type="date" data-filter="hasta" value="${safe(paymentFilter.hasta)}"></label><label>Método de cobro<select data-filter="forma">${methods.map(method => `<option${paymentFilter.forma === method ? ' selected' : ''}>${method}</option>`).join('')}</select></label><div class="filter-shortcuts"><span>Accesos rápidos</span><div><button class="button-secondary compact" data-action="today">Hoy</button><button class="button-secondary compact" data-action="month">Este mes</button></div></div></div>${table(['N° recibo', 'Fecha / hora', 'Socio', 'Monto', 'Forma de cobro', 'Período acreditado'], paymentRows(list))}</section>`;
}

function renderReports() {
    const list = data.pagos.filter(payment => { const date = new Date(payment.fechaPago); return date.getMonth() === Number(reportPeriod.mes) && date.getFullYear() === Number(reportPeriod.anio); });
    const total = list.reduce((sum, payment) => sum + Number(payment.monto), 0);
    const entries = data.asistencias.filter(item => { const date = new Date(item.fechaHora); return date.getMonth() === Number(reportPeriod.mes) && date.getFullYear() === Number(reportPeriod.anio); }).length;
    const key = `${reportPeriod.anio}-${String(Number(reportPeriod.mes) + 1).padStart(2, '0')}`;
    const covered = data.socios.filter(member => !member.eliminado && member.vencimiento && member.vencimiento.slice(0, 7) >= key).length;
    const gifts = list.filter(payment => payment.monto === 0 || payment.forma === 'Bonificación / Regalo').length;
    const methods = [...new Set(list.map(payment => payment.forma))].map(method => { const items = list.filter(payment => payment.forma === method); return `<tr><td><span class="method-tag">${safe(method)}</span></td><td>${items.length}</td><td class="amount">${money(items.reduce((sum, payment) => sum + Number(payment.monto), 0))}</td></tr>`; }).join('');
    const monthOptions = months.map((month, index) => `<option value="${index}"${Number(reportPeriod.mes) === index ? ' selected' : ''}>${month[0].toUpperCase() + month.slice(1)}</option>`).join('');
    const yearOptions = Array.from({ length: 5 }, (_, index) => new Date().getFullYear() + index).map(year => `<option${Number(reportPeriod.anio) === year ? ' selected' : ''}>${year}</option>`).join('');
    return `${header('Reportes Mensuales', 'Balance contable, evolución de membresías y concurrencia', '<button class="button-primary" data-action="print">Imprimir / guardar PDF</button>')}<form class="report-filters" data-form="report"><label>Mes<select name="mes">${monthOptions}</select></label><label>Año<select name="anio">${yearOptions}</select></label><span class="report-period">${months[reportPeriod.mes][0].toUpperCase() + months[reportPeriod.mes].slice(1)} ${reportPeriod.anio}</span></form>
        <section class="metric-grid report-metrics"><article class="metric"><span>Total recaudado</span><strong class="metric-green">${money(total)}</strong><small>${list.filter(payment => payment.monto > 0).length} cobros realizados</small></article><article class="metric"><span>Socios con cobertura</span><strong class="metric-blue">${covered}</strong><small>Cuotas vigentes en el período</small></article><article class="metric"><span>Total asistencias</span><strong class="metric-purple">${entries}</strong><small>Check-ins del período</small></article><article class="metric"><span>Bonificaciones</span><strong class="metric-amber">${gifts}</strong><small>Bonificaciones otorgadas</small></article></section>
        <section class="report-grid"><article class="panel"><div class="panel-heading"><div><h2>Recaudación por método de pago</h2><p>Desglose de operaciones del período</p></div></div>${table(['Método de cobro', 'Operaciones', 'Subtotal'], methods)}</article><article class="panel delinquency-panel"><h2>Estado de morosidad actual</h2><div class="delinquency-callout"><strong>${debtors().length} socios</strong><span>con cuota vencida o sin regularizar</span></div><p>Para contactar a los socios o regularizar sus cuotas, dirigite a la sección Deudores.</p><button class="text-button" data-view="deudores">Ir a Deudores →</button></article></section>`;
}

function renderSettings() {
    return `${header('Configuración del Sistema', 'Ajustes operativos, identidad del gimnasio, control de acceso y copias de respaldo')}<form class="settings-form" data-form="settings"><section class="panel settings-panel"><div class="section-heading"><h2>Parámetros de cuota y acceso</h2><p>Valores sugeridos para nuevas membresías y días de tolerancia permitidos.</p></div><div class="settings-grid"><label>Precio cuota mensual ($)<input name="precio" type="number" min="0" step="100" value="${safe(data.config.precio)}" required></label><label>Días de tolerancia de mora<input name="tolerancia" type="number" min="0" max="60" value="${safe(data.config.tolerancia)}" required></label></div></section>
        <section class="panel settings-panel"><div class="section-heading"><h2>Identidad del gimnasio</h2><p>Información visible en la barra lateral y en los comprobantes.</p></div><div class="settings-grid"><label class="wide-field">Nombre del establecimiento<input name="nombre" value="${safe(data.config.nombre)}" required></label><label>Teléfono / WhatsApp<input name="telefono" value="${safe(data.config.telefono)}"></label><label>Dirección / sucursal<input name="direccion" value="${safe(data.config.direccion)}"></label></div><div class="settings-actions"><button class="button-primary">Guardar cambios demo</button></div></section></form>
        <section class="panel settings-panel security-panel"><div class="section-heading"><h2>Seguridad y control de acceso</h2><p>En la aplicación de escritorio, Caja y Reportes pueden protegerse con una clave de administrador.</p></div><div class="security-note"><span class="badge warning">Acceso abierto en demo</span><span>Esta muestra no solicita ni almacena contraseñas. La protección real se configura en la aplicación instalada.</span></div></section>
        <section class="panel backup-panel"><div><h2>Copia de respaldo y restauración</h2><p>En esta demo, el respaldo exporta los datos locales del navegador en un archivo JSON.</p></div><div class="backup-actions"><button class="button-secondary" data-action="choose-restore">Seleccionar copia</button><button class="button-primary" data-action="backup">Exportar copia</button><input id="restoreFile" type="file" accept="application/json,.json" hidden></div></section>`;
}

const renderers = { inicio: renderHome, socios: renderMembers, acceso: renderAccess, deudores: renderDebtors, pagos: renderPayments, reportes: renderReports, configuracion: renderSettings };

function render() {
    root.innerHTML = renderers[view]();
    document.querySelectorAll('.nav-btn').forEach(button => button.classList.toggle('active', button.dataset.view === view));
    document.getElementById('branchName').textContent = data.config.nombre || 'Sucursal Central';
    updateClock();
}

function navigateTo(nextView, addHistory = true) {
    if (!renderers[nextView]) return;
    view = nextView;
    if (addHistory) history.pushState({ view }, '', `#${view}`);
    render();
}

window.addEventListener('popstate', event => {
    const hashView = location.hash.slice(1);
    const nextView = event.state?.view || (renderers[hashView] ? hashView : 'inicio');
    navigateTo(nextView, false);
});

function toast(message) {
    const element = document.getElementById('toast');
    element.textContent = message;
    element.classList.add('visible');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => element.classList.remove('visible'), 3000);
}

function updateClock() {
    const clock = document.getElementById('liveClock');
    if (clock) clock.textContent = new Intl.DateTimeFormat('es-AR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(new Date());
}

function openMember(id) {
    const form = document.getElementById('memberForm');
    form.reset();
    const member = id ? getMember(id) : null;
    document.getElementById('memberDialogTitle').textContent = member ? 'Editar socio' : 'Nuevo socio';
    for (const field of ['id', 'nombre', 'apellido', 'dni', 'telefono']) form.elements[field].value = member?.[field] || '';
    form.elements.codigoAcceso.value = member?.codigo || '';
    form.elements.vencimiento.value = member?.vencimiento || '';
    document.getElementById('memberDialog').showModal();
}

function openPayment(id) {
    const form = document.getElementById('paymentForm');
    const select = document.getElementById('paymentMember');
    const members = activeMembers();
    if (!members.length) return toast('No hay socios activos para cobrar.');
    select.innerHTML = members.map(member => `<option value="${member.id}"${Number(id) === member.id ? ' selected' : ''}>${safe(fullName(member))} · ${dateLabel(member.vencimiento)}</option>`).join('');
    form.elements.monto.value = data.config.precio;
    form.elements.montoTotal.value = data.config.precio;
    document.getElementById('paymentDialog').showModal();
}

function access(identifier) {
    const value = String(identifier || '').trim();
    if (!value) return;
    const member = data.socios.find(item => !item.eliminado && (String(item.id) === value || item.dni === value || item.codigo === value));
    if (!member) accessResult = { state: 'denied', title: 'SOCIO NO ENCONTRADO', detail: `No existe un socio registrado con ID, DNI o código “${value}”.` };
    else if (!member.activo) accessResult = { state: 'denied', title: 'ACCESO DENEGADO', detail: `ID #${member.id} · ${fullName(member)}. La cuenta se encuentra inactiva.` };
    else if (!member.vencimiento) accessResult = { state: 'denied', title: 'SIN CUOTA REGISTRADA', detail: `${fullName(member)} todavía no tiene pagos de cuota registrados.` };
    else {
        const late = Math.floor((fromISO(today()) - fromISO(member.vencimiento)) / 86400000);
        const recent = data.asistencias.some(item => item.socioId === member.id && Date.now() - new Date(item.fechaHora).getTime() >= 0 && Date.now() - new Date(item.fechaHora).getTime() < 120 * 60000);
        if (late > Number(data.config.tolerancia)) accessResult = { state: 'denied', title: 'CUOTA VENCIDA', detail: `${fullName(member)} venció el ${dateLabel(member.vencimiento)}. La tolerancia de ${data.config.tolerancia} días está agotada.` };
        else if (recent) accessResult = { state: 'denied', title: 'INGRESO YA REGISTRADO', detail: `${fullName(member)} ya registró un ingreso en las últimas 2 horas.` };
        else {
            data.asistencias.push({ id: data.nextAsistencia++, socioId: member.id, fechaHora: new Date().toISOString() });
            save();
            accessResult = late >= 0 ? { state: 'warning', title: 'ACCESO EN TOLERANCIA', detail: `${fullName(member)} venció el ${dateLabel(member.vencimiento)}. Avisar cobro en recepción.` } : { state: 'granted', title: 'ACCESO PERMITIDO', detail: `${fullName(member)} · cuota al día hasta el ${dateLabel(member.vencimiento)}.` };
        }
    }
    render();
}

function addMonths(date, amount) {
    const result = new Date(date);
    const day = result.getDate();
    result.setDate(1);
    result.setMonth(result.getMonth() + amount);
    result.setDate(Math.min(day, new Date(result.getFullYear(), result.getMonth() + 1, 0).getDate()));
    return result;
}

function submitMember(form) {
    const fields = new FormData(form);
    const id = Number(fields.get('id'));
    const code = String(fields.get('codigoAcceso')).trim();
    if (code && data.socios.some(member => !member.eliminado && member.codigo === code && member.id !== id)) return toast('Ese código de acceso ya está asignado.');
    const values = { nombre: String(fields.get('nombre')).trim(), apellido: String(fields.get('apellido')).trim(), dni: String(fields.get('dni')).trim(), telefono: String(fields.get('telefono')).trim(), codigo: code, vencimiento: String(fields.get('vencimiento')) };
    if (id) Object.assign(getMember(id), values);
    else data.socios.push({ id: data.nextSocio++, alta: today(), activo: true, ...values });
    save();
    document.getElementById('memberDialog').close();
    render();
    toast(id ? 'Socio actualizado en esta demo.' : 'Socio agregado a esta demo.');
}

function submitPayment(form) {
    const fields = new FormData(form);
    const member = getMember(fields.get('socioId'));
    if (!member) return toast('Seleccioná un socio válido.');
    const amountMonths = Math.max(1, Math.min(12, Number(fields.get('meses')) || 1));
    const todayDate = fromISO(today());
    const oldExpiry = fromISO(member.vencimiento);
    const start = oldExpiry && oldExpiry >= todayDate ? new Date(oldExpiry.setDate(oldExpiry.getDate() + 1)) : todayDate;
    const end = addMonths(start, amountMonths);
    end.setDate(end.getDate() - 1);
    const payment = { id: data.nextPago++, socioId: member.id, fechaPago: new Date().toISOString(), monto: Number(fields.get('monto')) || 0, montoTotal: Number(fields.get('montoTotal')) || 0, forma: String(fields.get('forma')), desde: isoDate(start), hasta: isoDate(end) };
    data.pagos.unshift(payment);
    member.vencimiento = payment.hasta;
    member.activo = true;
    save();
    document.getElementById('paymentDialog').close();
    render();
    toast(`Cobro demo registrado. Cuota cubierta hasta ${dateLabel(member.vencimiento)}.`);
}

function exportBackup() {
    const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `GymManager_Demo_${today()}.json`;
    link.click();
    URL.revokeObjectURL(url);
    toast('Copia demo descargada.');
}

function resetDemo() {
    if (!confirm('¿Restablecer los datos de ejemplo? Se perderán los cambios guardados en este navegador.')) return;
    data = sampleData();
    accessResult = null;
    paymentFilter = { desde: `${today().slice(0, 8)}01`, hasta: today(), forma: 'Todos' };
    save(); render(); toast('Demo restablecida con los datos de ejemplo.');
}

document.addEventListener('click', event => {
    const nav = event.target.closest('[data-view]');
    if (nav) { navigateTo(nav.dataset.view); return; }
    const button = event.target.closest('[data-action]');
    if (!button) return;
    const id = Number(button.dataset.id);
    switch (button.dataset.action) {
        case 'new-member': openMember(); break;
        case 'edit': openMember(id); break;
        case 'pay': openPayment(button.dataset.id); break;
        case 'close-dialog': button.closest('dialog').close(); break;
        case 'quick-access': access(button.dataset.code); break;
        case 'toggle': { const member = getMember(id); member.activo = !member.activo; save(); render(); toast(`${fullName(member)}: socio ${member.activo ? 'activado' : 'pausado'} en la demo.`); break; }
        case 'refresh': render(); toast('Lista actualizada.'); break;
        case 'today': paymentFilter = { ...paymentFilter, desde: today(), hasta: today() }; render(); break;
        case 'month': paymentFilter = { ...paymentFilter, desde: `${today().slice(0, 8)}01`, hasta: today() }; render(); break;
        case 'print': window.print(); break;
        case 'backup': exportBackup(); break;
        case 'choose-restore': document.getElementById('restoreFile').click(); break;
        case 'reset-demo': resetDemo(); break;
    }
});

document.addEventListener('submit', event => {
    const form = event.target.closest('[data-form]');
    if (!form) return;
    event.preventDefault();
    switch (form.dataset.form) {
        case 'access': access(new FormData(form).get('identifier')); break;
        case 'member': submitMember(form); break;
        case 'payment': submitPayment(form); break;
        case 'settings': {
            const fields = new FormData(form);
            data.config = { ...data.config, nombre: String(fields.get('nombre')).trim(), telefono: String(fields.get('telefono')).trim(), direccion: String(fields.get('direccion')).trim(), precio: Math.max(0, Number(fields.get('precio'))), tolerancia: Math.max(0, Number(fields.get('tolerancia'))) };
            save(); render(); toast('Configuración guardada en este navegador.');
            break;
        }
    }
});

document.addEventListener('input', event => {
    if (event.target.id === 'memberSearch') {
        const term = event.target.value.trim().toLocaleLowerCase('es');
        const list = data.socios.filter(member => !member.eliminado && `${fullName(member)} ${member.dni} ${member.id}`.toLocaleLowerCase('es').includes(term));
        document.querySelector('.data-table tbody').innerHTML = personRows(list) || '<tr><td colspan="6" class="empty-cell">No se encontraron socios.</td></tr>';
        document.getElementById('memberCount').textContent = `${list.length} socios`;
    }
    if (event.target.id === 'debtorSearch') {
        const term = event.target.value.trim().toLocaleLowerCase('es');
        const list = debtors().filter(member => `${fullName(member)} ${member.dni}`.toLocaleLowerCase('es').includes(term));
        document.querySelector('.data-table tbody').innerHTML = debtorRows(list) || '<tr><td colspan="6" class="empty-cell">No se encontraron deudores.</td></tr>';
    }
});

document.addEventListener('change', event => {
    if (event.target.matches('[data-filter]')) { paymentFilter[event.target.dataset.filter] = event.target.value; render(); }
    if (event.target.closest('[data-form="report"]')) {
        const fields = new FormData(event.target.form);
        reportPeriod = { mes: Number(fields.get('mes')), anio: Number(fields.get('anio')) };
        render();
    }
    if (event.target.id === 'restoreFile' && event.target.files[0]) {
        event.target.files[0].text().then(text => {
            const restored = JSON.parse(text);
            if (!restored.socios || !restored.pagos || !restored.asistencias || !restored.config) throw new Error('Invalid demo backup');
            data = restored; save(); render(); toast('Datos demo restaurados desde el archivo.');
        }).catch(() => toast('No se pudo leer el archivo de respaldo demo.'));
    }
});

setInterval(updateClock, 1000);
view = renderers[location.hash.slice(1)] ? location.hash.slice(1) : 'inicio';
history.replaceState({ view }, '', location.href);
save();
render();