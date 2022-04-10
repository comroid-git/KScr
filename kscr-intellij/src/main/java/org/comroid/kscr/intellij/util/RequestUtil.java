package org.comroid.kscr.intellij.util;

import com.google.gson.Gson;
import com.google.gson.JsonElement;
import com.intellij.util.io.HttpRequests;

import java.io.IOException;

public final class RequestUtil {
    public static JsonElement parseJson(HttpRequests.Request request) throws IOException {
        String data = request.readString();
        return new Gson().toJsonTree(data);
    }
}
