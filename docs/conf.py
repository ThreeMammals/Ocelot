# Configuration file for the Sphinx documentation builder.
#
# For the full list of built-in configuration values, see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Project information -----------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#project-information

project = 'Ocelot Gateway'
copyright = ' 2016-2024, ThreeMammals Ocelot team'
author = 'Tom Pallister, Raman Maksimchuk'
release = '23.3'

# -- General configuration ---------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#general-configuration

extensions = [
]

templates_path = ['_templates']
exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store']

# -- Options for HTML output -------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#options-for-html-output

# https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-html_theme
html_theme = 'alabaster'

# https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-html_static_path
html_static_path = ['_static']
html_css_files = ['overrides.css']
