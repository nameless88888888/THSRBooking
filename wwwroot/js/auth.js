// 儲存登入資訊（登入成功時呼叫）
function saveAuth(loginResponse) {
    const auth = {
        token: loginResponse.token,
        expireAt: loginResponse.expireAt,
        userId: loginResponse.userId,
        name: loginResponse.name,
        email: loginResponse.email
    };
    localStorage.setItem("thsr_auth", JSON.stringify(auth));
}

// 取得目前登入資訊
function getAuth() {
    const raw = localStorage.getItem("thsr_auth");
    if (!raw) return null;
    try {
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

// 登出用
function clearAuth() {
    localStorage.removeItem("thsr_auth");
}

// 產生帶 JWT 的 headers
function getAuthHeaders() {
    const auth = getAuth();
    if (!auth || !auth.token) return {};
    return {
        "Authorization": "Bearer " + auth.token
    };
}

// 更新頁面右上角登入/登出 UI（可依你自己的版面調整）
function refreshAuthUI() {
    const area = document.getElementById("authArea");
    if (!area) return;
    const auth = getAuth();

    if (auth && auth.name) {
        area.innerHTML = `
            <span class="userText">${auth.name} 您好</span>
            <a href="#" id="logoutLink">登出</a>
        `;
        document.getElementById("logoutLink").addEventListener("click", (e) => {
            e.preventDefault();
            clearAuth();
            window.location.href = "login.html";
        });
    } else {
        area.innerHTML = `<a href="login.html">登入</a>`;
    }
}
