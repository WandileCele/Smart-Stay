/* =========================================================
   SESSION IDLE TIMEOUT

   - Watches for user activity (mouse, keyboard, scroll, touch)
   - After X minutes of no activity, shows a warning modal
     with a countdown
   - "Stay Signed In" resets the timer
   - If the countdown reaches 0, the user is logged out and
     redirected to the login page
========================================================= */

(function () {
    "use strict";

    var script = document.currentScript;

    var idleMinutes = parseFloat(script.getAttribute("data-idle-minutes")) || 14;
    var warningSeconds = parseInt(script.getAttribute("data-warning-seconds"), 10) || 60;

    var idleLimitMs = idleMinutes * 60 * 1000;

    var idleTimer = null;
    var countdownTimer = null;
    var secondsLeft = warningSeconds;

    var modalEl = document.getElementById("sessionTimeoutModal");
    var countdownEl = document.getElementById("timeoutCountdown");
    var stayBtn = document.getElementById("staySignedInBtn");
    var autoLogoutForm = document.getElementById("autoLogoutForm");

    var bsModal = (modalEl && window.bootstrap)
        ? new bootstrap.Modal(modalEl)
        : null;


    /* ============================================================
       START / RESTART THE IDLE COUNTDOWN
    ============================================================ */

    function startIdleTimer() {
        clearTimeout(idleTimer);
        idleTimer = setTimeout(showWarning, idleLimitMs);
    }


    /* ============================================================
       SHOW THE "YOU'RE ABOUT TO BE LOGGED OUT" MODAL
    ============================================================ */

    function showWarning() {
        secondsLeft = warningSeconds;

        if (countdownEl) {
            countdownEl.textContent = secondsLeft;
        }

        if (bsModal) {
            bsModal.show();
        }

        countdownTimer = setInterval(function () {
            secondsLeft--;

            if (countdownEl) {
                countdownEl.textContent = secondsLeft;
            }

            if (secondsLeft <= 0) {
                clearInterval(countdownTimer);
                logoutNow();
            }
        }, 1000);
    }


    /* ============================================================
       LOG THE USER OUT
    ============================================================ */

    function logoutNow() {
        if (autoLogoutForm) {
            autoLogoutForm.submit();
        } else {
            window.location.href = "/Account/Login";
        }
    }


    /* ============================================================
       USER CLICKED "STAY SIGNED IN"
    ============================================================ */

    function resetTimers() {
        clearInterval(countdownTimer);

        if (bsModal) {
            bsModal.hide();
        }

        startIdleTimer();
    }

    if (stayBtn) {
        stayBtn.addEventListener("click", resetTimers);
    }


    var activityEvents = ["mousemove", "mousedown", "keydown", "scroll", "touchstart"];

    activityEvents.forEach(function (evt) {
        document.addEventListener(evt, function () {
            var modalIsVisible = modalEl && modalEl.classList.contains("show");

            if (!modalIsVisible) {
                startIdleTimer();
            }
        }, { passive: true });
    });




    startIdleTimer();

})();