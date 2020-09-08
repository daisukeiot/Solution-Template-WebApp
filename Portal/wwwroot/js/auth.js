function initAuth(clientId, tenant, ){

    if (clientId == "") {
        console.log("TSI Client ID is emptry");
        return;
    }

    authContext = new AuthenticationContext({
        clientId: clientId,
        cacheLocation: 'localStorage',
        tenant: tenant
    });



    if (authContext.isCallback(window.location.hash)) {

        // Handle redirect after token requests
        authContext.handleWindowCallback();
        var err = authContext.getLoginError();
        if (err) {
            // TODO: Handle errors signing in and getting tokens
            document.getElementById('tsiLoginContext').textContent = err;
            document.getElementById('tsiLogin').style.display = "block";
        }
    } else {
        var user = authContext.getCachedUser();
        if (user) {
            document.getElementById('tsiLogin').style.display = "block";
            document.getElementById('btnTsiLogout').innerHTML = '<i class="fas fa-sign-out-alt"></i>Logout : ' + user.userName;
            document.getElementById('tsiUserInfo').style.display = "block";
                
        } else {
            document.getElementById('btnTsiLogout').innerHTML = '<i class="fas fa-sign-in-alt"></i>Not signed in.';
        }
    }

    authContext.getTsiToken = function(){
        document.getElementById('tsiLoginContext').textContent = 'Getting tsi token...';
        
        // Get an access token to the Microsoft TSI API
        var promise = new Promise(function(resolve,reject){
            authContext.acquireToken(
            '120d688d-1518-4cf7-bd38-182f158850b6',
            function (error, token) {

                if (error || !token) {
                    document.getElementById('tsiLoginContext').textContent = error;
                    document.getElementById('tsiLogin').style.display = "block";
                    return;
                }

                document.getElementById('tsiLoginContext').textContent = '';
                document.getElementById('tsiLogin').style.display = "none";
                resolve(token);
                }
            );
        });
        
        return promise;
    }
}