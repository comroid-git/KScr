plugins {
    id("org.jetbrains.intellij") version "1.3.0"
    id("antlr") // TODO: Use the plugin to build the parser automatically - requires splitting parser and lexer
    java
}

group = "org.comroid.kscr"
version = "0.0.1"

repositories {
    mavenCentral()
}

dependencies {
    implementation("org.antlr:antlr4-runtime:4.9.3")
    implementation("org.antlr:antlr4-intellij-adaptor:0.1")
    //implementation("org.antlr:antlr4-master:4.9.3")
    
    testImplementation("org.junit.jupiter:junit-jupiter-api:5.6.0")
    testRuntimeOnly("org.junit.jupiter:junit-jupiter-engine")
}

// See https://github.com/JetBrains/gradle-intellij-plugin/
intellij {
    version.set("2021.3")
    plugins.add("com.intellij.java")
    plugins.add("yaml")
}
tasks {
    patchPluginXml {
        changeNotes.set("""
            First version.       """.trimIndent())
    }
}
tasks.getByName<Test>("test") {
    useJUnitPlatform()
}