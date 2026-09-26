
document.addEventListener('DOMContentLoaded', function () {
    
    var counters = document.querySelectorAll('.stats-section .stat-number');

    if (counters.length > 0) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    var counter = entry.target;
                    var target = parseInt(counter.getAttribute('data-count') || '0', 10);
                    animateCounter(counter, target);
                    observer.unobserve(counter);
                }
            });
        }, { threshold: 0.5 });

        counters.forEach(function (counter) {
            observer.observe(counter);
        });
    }
});

function animateCounter(element, target) {
    var current = 0;
    var increment = Math.ceil(target / 50);

    var timer = setInterval(function () {
        current += increment;
        if (current >= target) {
            current = target;
            clearInterval(timer);
        }
        element.textContent = current;
    }, 40);
}


function selectRole(role) {
    var selectedRole = document.getElementById('selectedRole');
    if (selectedRole) {
        selectedRole.value = role;
    }

    var roleOptions = document.querySelectorAll('.role-option');
    for (var i = 0; i < roleOptions.length; i++) {
        var el = roleOptions[i];
        el.classList.remove('selected');
        if (el.dataset.role === role) {
            el.classList.add('selected');
        }
    }
}


function togglePassword(inputId, toggleId) {
    inputId = inputId || 'passwordInput';
    toggleId = toggleId || 'passwordToggleIcon';

    var passwordInput = document.getElementById(inputId);
    var toggleIcon = document.getElementById(toggleId);

    if (!passwordInput || !toggleIcon) return;

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.className = 'fas fa-eye-slash';
    } else {
        passwordInput.type = 'password';
        toggleIcon.className = 'fas fa-eye';
    }
}

function showToast(message, type) {
    type = type || 'success';
    var colors = {
        success: 'linear-gradient(135deg, #48BB78, #38A169)',
        error: 'linear-gradient(135deg, #FC8181, #E53E3E)',
        warning: 'linear-gradient(135deg, #F6AD55, #ED8936)',
        info: 'linear-gradient(135deg, #63B3ED, #3182CE)'
    };

    var toast = document.createElement('div');
    toast.style.cssText =
        'position: fixed; bottom: 24px; right: 24px; ' +
        'background: ' + (colors[type] || colors.info) + '; ' +
        'color: white; padding: 16px 24px; border-radius: 12px; ' +
        'font-weight: 500; box-shadow: 0 8px 30px rgba(0, 0, 0, 0.2); ' +
        'transform: translateY(100px); opacity: 0; ' +
        'transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1); ' +
        'z-index: 9999; max-width: 400px;';
    toast.textContent = message || 'Notification';
    document.body.appendChild(toast);

    requestAnimationFrame(function () {
        toast.style.transform = 'translateY(0)';
        toast.style.opacity = '1';
    });

    setTimeout(function () {
        toast.style.transform = 'translateY(100px)';
        toast.style.opacity = '0';
        setTimeout(function () {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 400);
    }, 4000);
}
console.log('AbilityConnect BD - Empowering Every Ability');