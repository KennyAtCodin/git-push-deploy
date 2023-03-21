//@req(user, repo, token, callbackUrl, scriptName, envName)
import org.apache.commons.httpclient.HttpClient;
import org.apache.commons.httpclient.methods.GetMethod;
import org.apache.commons.httpclient.methods.DeleteMethod;
import org.apache.commons.httpclient.methods.PostMethod;
import org.apache.commons.httpclient.UsernamePasswordCredentials;
import org.apache.commons.httpclient.methods.StringRequestEntity;
import org.apache.commons.httpclient.auth.AuthScope;
import com.hivext.api.core.utils.JSONUtils;
import java.io.InputStreamReader;
import java.io.BufferedReader;

var client = new HttpClient();

//Parsing repo url
var origRepo = repo;
var domain = "github.com";
if (repo.indexOf(".git") > -1) repo = repo.split(".git")[0];
if (repo.indexOf("/") > -1) {
    var arr = repo.split("/");
    repo = arr.pop();
    if (repo == "") repo = arr.pop();
    user = arr.pop();
    domain = arr.pop() || domain;
}



//Get list of hooks
var gitApiUrl =  "https://" + domain + "/api/v4/projects/" + repo + "/hooks";

//Create a new hook
var post = new PostMethod(gitApiUrl);


//Hook request params
var params =  {
    "push_events": true,
    "merge_requests_events": true,
    "url": callbackUrl
};

//Authentication for GitLab
if (!IS_GITHUB) post.addRequestHeader("PRIVATE-TOKEN", token);

resp = exec(post, params);
if (resp.result != 0) return resp;
var newHook = eval("(" + resp.response + ")");

return {
    result: 0,
    hook: newHook
};

function exec(method, params) {
    if (params) {
        var requestEntity = new StringRequestEntity(JSONUtils.jsonStringify(params), "application/json", "UTF-8");
        method.setRequestEntity(requestEntity);
    }
    var status = client.executeMethod(method),
        response = "",
        result = 0,
        type = null,
        error = null;
    if (status == 200 || status == 201) {
        var br = new BufferedReader(new InputStreamReader(method.getResponseBodyAsStream())),
            line;
        while ((line = br.readLine()) != null) {
            response = response + line;
        }
    } else {
        error = "ERROR: " + method.getStatusLine() + " -> user:" + user + " token:" + token + " repo:" + repo;
        if (status == 401) error = "Wrong username or/and token. Please, double check your entries.";
        result = status;
        type = "error";
        response = JSONUtils.jsonStringify(params);
    }
    method.releaseConnection();
    return {
        result: result,
        response: response,
        type: type,
        message: error
    };
}
