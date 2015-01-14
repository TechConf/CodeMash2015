package org.oclc.plugin

import org.gradle.api.*
import org.gradle.api.tasks.testing.*

class integrationTestPlugin implements Plugin<Project> {
    void apply(Project project) {
        project.sourceSets {
            integrationTest {
                java.srcDir project.file('src/integtest/java')
            }
        }

        project.configurations {
            integrationTestCompile.extendsFrom testCompile
            integrationTestRuntime.extendsFrom testRuntime
        }


        project.task('integrationTest', type: Test) {
            description = 'Run integration tests (located in src/integTest/...).'
            testClassesDir = project.sourceSets.integrationTest.output.classesDir
            classpath = project.sourceSets.integrationTest.runtimeClasspath
        }
    }
}
