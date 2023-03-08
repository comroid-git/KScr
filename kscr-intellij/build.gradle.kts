plugins {
    id("java")
    id("org.jetbrains.intellij") version "1.13.0"
}

group = "org.comroid.kscr"
version = "0.1.0"

repositories {
    maven("https://maven.comroid.org")
    mavenCentral()
}

dependencies {
    if (findProject(":api") != null)
        implementation(project(":api"))
    else implementation("org.comroid:api:1.+")
    implementation("org.antlr:antlr4-runtime:4.11.1")
    implementation("org.antlr:antlr4-intellij-adaptor:0.1")
}

// Configure Gradle IntelliJ Plugin
// Read more: https://plugins.jetbrains.com/docs/intellij/tools-gradle-intellij-plugin.html
intellij {
    version.set("2021.3.3")
    type.set("IC") // Target IDE Platform

    plugins.set(listOf("com.intellij.java", "yaml"))
}

tasks {
    // Set the JVM compatibility versions
    withType<JavaCompile> {
        sourceCompatibility = "11"
        targetCompatibility = "11"
    }

    patchPluginXml {
        sinceBuild.set("213")
        untilBuild.set("223.*")
    }

    signPlugin {
        certificateChain.set(System.getenv("CERTIFICATE_CHAIN"))
        privateKey.set(System.getenv("PRIVATE_KEY"))
        password.set(System.getenv("PRIVATE_KEY_PASSWORD"))
    }

    publishPlugin {
        token.set(System.getenv("PUBLISH_TOKEN"))
    }
}
