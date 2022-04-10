package org.comroid.kscr.intellij.sdk.module.wizard.panel;

import com.google.gson.JsonElement;
import com.intellij.ide.util.PropertiesComponent;
import com.intellij.openapi.actionSystem.AnAction;
import com.intellij.openapi.actionSystem.AnActionEvent;
import com.intellij.openapi.application.AccessToken;
import com.intellij.openapi.application.ApplicationManager;
import com.intellij.openapi.fileChooser.FileChooser;
import com.intellij.openapi.fileChooser.FileChooserDescriptor;
import com.intellij.openapi.fileChooser.FileChooserFactory;
import com.intellij.openapi.fileChooser.PathChooserDialog;
import com.intellij.openapi.projectRoots.Sdk;
import com.intellij.openapi.vfs.VfsUtil;
import com.intellij.openapi.vfs.VirtualFile;
import com.intellij.ui.components.ActionLink;
import com.intellij.util.download.DownloadableFileDescription;
import com.intellij.util.download.DownloadableFileService;
import com.intellij.util.download.FileDownloader;
import com.intellij.util.io.HttpRequests;
import com.intellij.util.io.ZipUtil;
import org.comroid.kscr.intellij.util.RequestUtil;

import javax.swing.*;
import java.awt.*;
import java.io.FilenameFilter;
import java.io.IOException;
import java.nio.file.Path;
import java.util.List;
import java.util.stream.StreamSupport;

public class KScrInstallSdkPanel extends JPanel {
    public static final String LAST_USED_KSCR_HOME = "LAST_USED_KSCR_HOME";
    private ActionLink myDownloadLink;
    private JPanel myRoot;
    private KScrInstallSdkComboBox mySdkComboBox;

    public KScrInstallSdkPanel() {
        super(new BorderLayout());
        add(myRoot, BorderLayout.CENTER);
    }

    private void createUIComponents() {
        myDownloadLink = new ActionLink("Download and Install KScr SDK", event -> {
                FileChooserDescriptor descriptor = new FileChooserDescriptor(false, true, false, false, false, false);
                PathChooserDialog pathChooser = FileChooserFactory.getInstance()
                        .createPathChooser(descriptor, null, KScrInstallSdkPanel.this);
                pathChooser.choose(VfsUtil.getUserHomeDir(), new FileChooser.FileChooserConsumer() {
                    @Override
                    public void cancelled() {
                    }

                    @Override
                    public void consume(List<VirtualFile> virtualFiles) {
                        if (virtualFiles.size() == 1) {
                            VirtualFile dir = virtualFiles.get(0);
                            String dirName = dir.getName();
                            try {
                                if (!dirName.toLowerCase().contains("smalltalk") && !dirName.toLowerCase().contains("redline")) {
                                    try {
                                        dir = dir.createChildDirectory(this, "RedlineSmalltalk");
                                    } catch (IOException e) {//
                                    }
                                }
                                var url = StreamSupport.stream(HttpRequests.request("https://api.github.com/repos/comroid-git/KScr/releases")
                                                .accept("application/json")
                                                .connect(RequestUtil::parseJson).getAsJsonArray()
                                                .get(0).getAsJsonObject()
                                                .get("assets").getAsJsonArray()
                                                .spliterator(), false)
                                        .map(JsonElement::getAsJsonObject)
                                        .filter(asset -> asset.get("name").getAsString().endsWith(".zip"))
                                        .findFirst()
                                        .map(asset -> asset.get("browser_download_url").getAsString())
                                        .orElseThrow(() -> new RuntimeException("Could not find latest KScr release"));
                                var split = url.lastIndexOf('/');
                                String urlDir = url.substring(0, split);
                                String urlFile = url.substring(split + 1);
                                var fileService = DownloadableFileService.getInstance();
                                var fileDescription = fileService.createFileDescription(urlDir, urlFile);
                                var downloader = fileService.createDownloader(List.of(fileDescription), "KScr Distribution");
                                var files = downloader.download(Path.of(System.getProperty("java.io.tmpdir"), "kscr-dist/").toFile());
                                if (files.size() == 1) {
                                    ZipUtil.extract(files.get(0).first.toPath(), dir.toNioPath(), (parent, file) -> true);
                                    PropertiesComponent.getInstance().setValue(LAST_USED_KSCR_HOME, dir.getPath());
                                }
                            } catch (IOException e) {
                                throw new RuntimeException(e);
                            }
                        }
                    }});
        });
    }

    public String getSdkName() {
        final Sdk selectedSdk = mySdkComboBox.getSelectedSdk();
        return selectedSdk == null ? null : selectedSdk.getName();
    }

    public Sdk getSdk() {
        return mySdkComboBox.getSelectedSdk();
    }

    public void setSdk(Sdk sdk) {
        mySdkComboBox.getComboBox().setSelectedItem(sdk);
    }
}
