// Limita escritura y pegado a cinco dígitos. El servidor valida la misma regla.
const employeeNumber = document.getElementById('Number');
if (employeeNumber) {
    employeeNumber.addEventListener('input', () => {
        employeeNumber.value = employeeNumber.value.replace(/[^0-9]/g, '').slice(0, 5);
    });
}
